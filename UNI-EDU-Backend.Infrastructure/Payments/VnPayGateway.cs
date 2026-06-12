using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using UNI_EDU_Backend.Application.Interfaces;

namespace UNI_EDU_Backend.Infrastructure.Payments;

// VNPay v2.1.0 redirect-flow gateway. Initiate = build a signed URL (HMAC-SHA512 over the
// URL-encoded, alphabetically-sorted vnp_* query string). No outbound HTTP at create time.
public class VnPayGateway(IOptions<VnPayOptions> options, IHttpContextAccessor httpContext) : IVnPayGateway
{
    private static readonly TimeZoneInfo VietnamTime = ResolveVietnamTime();
    private readonly VnPayOptions _options = options.Value;
    private readonly IHttpContextAccessor _httpContext = httpContext;

    public string Method => "vnpay";

    public Task<PaymentCreationResult> CreatePaymentAsync(PaymentCreationCommand command, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTime);
        var ipAddr = ResolveIpv4(_httpContext.HttpContext?.Connection?.RemoteIpAddress);

        // VNPay sends amount in subunits (×100) — whole VND × 100.
        var amountSubunits = (long)command.Amount * 100;

        // SortedDictionary with ordinal compare → matches VNPay's reference (Java TreeMap).
        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _options.TmnCode,
            ["vnp_Amount"] = amountSubunits.ToString(),
            ["vnp_CreateDate"] = nowVn.ToString("yyyyMMddHHmmss"),
            ["vnp_CurrCode"] = "VND",
            ["vnp_IpAddr"] = ipAddr,
            ["vnp_Locale"] = "vn",
            ["vnp_OrderInfo"] = command.OrderInfo,
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = _options.ReturnUrl,
            ["vnp_TxnRef"] = command.OrderId
        };

        var signData = BuildEncodedQueryString(vnpParams);
        var signature = HmacSha512Hex(signData, _options.HashSecret);

        var payUrl = $"{_options.PayUrl}?{signData}&vnp_SecureHash={signature}";
        return Task.FromResult(new PaymentCreationResult(payUrl));
    }

    // IMPORTANT: vnpFields must carry the values EXACTLY as they appear in the URL (still
    // URL-encoded). VNPay signs the encoded query string; ASP.NET's Request.Query decodes
    // values AND does not turn '+' back into a space, so re-encoding here would corrupt any
    // field containing spaces (e.g. vnp_OrderInfo). Hash the raw encoded values as-is.
    public bool VerifyCallbackSignature(IReadOnlyDictionary<string, string> vnpFields, string providedHash)
    {
        var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in vnpFields)
        {
            if (string.Equals(kv.Key, "vnp_SecureHash", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.Equals(kv.Key, "vnp_SecureHashType", StringComparison.OrdinalIgnoreCase)) continue;
            sorted[kv.Key] = kv.Value ?? string.Empty;
        }

        // Values are already URL-encoded → join key=value directly (no re-encoding).
        var signData = string.Join("&", sorted.Select(kv => $"{kv.Key}={kv.Value}"));
        var expected = HmacSha512Hex(signData, _options.HashSecret);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes((providedHash ?? string.Empty).Trim().ToLowerInvariant()));
    }

    // WebUtility.UrlEncode matches Java's URLEncoder (space → '+', uppercase %XX) — VNPay
    // verifies signatures by re-encoding with the same scheme. Using the wrong encoder is
    // the #1 cause of "invalid checksum" failures.
    private static string BuildEncodedQueryString(IDictionary<string, string> sorted)
    {
        var sb = new StringBuilder();
        var first = true;
        foreach (var kv in sorted)
        {
            if (!first) sb.Append('&');
            sb.Append(kv.Key).Append('=').Append(WebUtility.UrlEncode(kv.Value));
            first = false;
        }
        return sb.ToString();
    }

    // VNPay expects an IPv4 vnp_IpAddr. Map loopback/IPv6 (e.g. "::1", "::ffff:127.0.0.1") to IPv4.
    private static string ResolveIpv4(System.Net.IPAddress? ip)
    {
        if (ip is null) return "127.0.0.1";
        if (System.Net.IPAddress.IsLoopback(ip)) return "127.0.0.1";
        if (ip.IsIPv4MappedToIPv6) return ip.MapToIPv4().ToString();
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) return ip.ToString();
        return "127.0.0.1";
    }

    private static string HmacSha512Hex(string data, string key)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static TimeZoneInfo ResolveVietnamTime()
    {
        // .NET 6+ handles IANA names cross-platform, but fall back to the Windows id if needed.
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
    }

    private void EnsureConfigured()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(_options.TmnCode)) missing.Add(nameof(VnPayOptions.TmnCode));
        if (string.IsNullOrWhiteSpace(_options.HashSecret)) missing.Add(nameof(VnPayOptions.HashSecret));
        if (string.IsNullOrWhiteSpace(_options.PayUrl)) missing.Add(nameof(VnPayOptions.PayUrl));
        if (string.IsNullOrWhiteSpace(_options.ReturnUrl)) missing.Add(nameof(VnPayOptions.ReturnUrl));

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"VNPay gateway is not configured. Missing: {string.Join(", ", missing)}. " +
                "Set them under the 'VnPay' config section, or as environment variables VnPay__TmnCode, VnPay__HashSecret, VnPay__ReturnUrl, etc. (double underscore).");

        if (!Uri.TryCreate(_options.ReturnUrl, UriKind.Absolute, out var returnUri) ||
            (returnUri.Scheme != Uri.UriSchemeHttp && returnUri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("VnPay ReturnUrl must be an absolute http(s) URL.");
    }
}

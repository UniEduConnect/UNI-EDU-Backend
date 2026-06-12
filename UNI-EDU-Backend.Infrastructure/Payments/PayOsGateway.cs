using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UNI_EDU_Backend.Application.DTOs.Wallets;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces;

namespace UNI_EDU_Backend.Infrastructure.Payments;

// PayOS redirect-flow gateway. Initiate = POST a signed payment-request, return checkoutUrl.
// The numeric orderCode comes from WalletService (it sets command.OrderId to the orderCode
// string for payos, which is also stored as the transaction's ProviderRef for webhook lookup).
public class PayOsGateway(HttpClient http, IOptions<PayOsOptions> options) : IPayOsGateway
{
    private readonly HttpClient _http = http;
    private readonly PayOsOptions _options = options.Value;

    public string Method => "payos";

    public async Task<PaymentCreationResult> CreatePaymentAsync(PaymentCreationCommand command, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        if (!long.TryParse(command.OrderId, out var orderCode))
            throw new BadRequestException("PayOS order code must be numeric.");

        var amount = (long)command.Amount;
        // PayOS caps description at 25 chars.
        var description = (command.OrderInfo ?? "Nap tien UNI-EDU").Trim();
        if (description.Length > 25) description = description[..25];

        // Signature over the alphabetically-sorted required fields.
        var signData = $"amount={amount}&cancelUrl={_options.CancelUrl}&description={description}&orderCode={orderCode}&returnUrl={_options.ReturnUrl}";
        var signature = HmacSha256Hex(_options.ChecksumKey, signData);

        var requestBody = new
        {
            orderCode,
            amount,
            description,
            returnUrl = _options.ReturnUrl,
            cancelUrl = _options.CancelUrl,
            signature
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, _options.CreateUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        req.Headers.Add("x-client-id", _options.ClientId);
        req.Headers.Add("x-api-key", _options.ApiKey);

        using var resp = await _http.SendAsync(req, cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var code = root.TryGetProperty("code", out var c) ? c.GetString() : null;
        if (code != "00" || !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object
            || !data.TryGetProperty("checkoutUrl", out var url) || string.IsNullOrWhiteSpace(url.GetString()))
        {
            var desc = root.TryGetProperty("desc", out var d) ? d.GetString() : "unknown";
            throw new BadRequestException($"PayOS rejected the payment request: {desc}");
        }

        return new PaymentCreationResult(url.GetString()!);
    }

    public bool VerifyWebhookSignature(PayOsWebhookPayload payload)
    {
        if (string.IsNullOrWhiteSpace(_options.ChecksumKey) || string.IsNullOrWhiteSpace(payload.Signature))
            return false;
        if (payload.Data.ValueKind != JsonValueKind.Object)
            return false;

        // Recompute HMAC-SHA256 over the data object's alphabetically-sorted key=value pairs.
        var pairs = payload.Data.EnumerateObject()
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => $"{p.Name}={ValueToString(p.Value)}");
        var canonical = string.Join("&", pairs);
        var expected = HmacSha256Hex(_options.ChecksumKey, canonical);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes((payload.Signature ?? string.Empty).Trim().ToLowerInvariant()));
    }

    private static string ValueToString(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.String => v.GetString() ?? string.Empty,
        JsonValueKind.Null => string.Empty,
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => v.GetRawText()
    };

    private static string HmacSha256Hex(string key, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private void EnsureConfigured()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(_options.ClientId)) missing.Add("ClientId");
        if (string.IsNullOrWhiteSpace(_options.ApiKey)) missing.Add("ApiKey");
        if (string.IsNullOrWhiteSpace(_options.ChecksumKey)) missing.Add("ChecksumKey");
        if (string.IsNullOrWhiteSpace(_options.ReturnUrl)) missing.Add("ReturnUrl");
        if (string.IsNullOrWhiteSpace(_options.CancelUrl)) missing.Add("CancelUrl");
        if (missing.Count > 0)
            throw new BadRequestException(
                $"PayOS gateway is not configured. Missing: {string.Join(", ", missing)}. " +
                "Set them under the 'PayOs' config section, or as env vars PayOs__ClientId, PayOs__ApiKey, PayOs__ChecksumKey, PayOs__ReturnUrl, PayOs__CancelUrl.");
    }
}

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using UNI_EDU_Backend.Application.DTOs.Wallets;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces;

namespace UNI_EDU_Backend.Infrastructure.Payments;

// Momo v2 (AIO) gateway. Signing follows Momo's documented HMAC-SHA256 over a fixed,
// alphabetically-ordered field list using the merchant secret key.
public class MomoGateway(HttpClient httpClient, IOptions<MomoOptions> options) : IMomoGateway
{
    private const string RequestType = "captureWallet";
    private readonly HttpClient _httpClient = httpClient;
    private readonly MomoOptions _options = options.Value;

    public string Method => "momo";

    public async Task<PaymentCreationResult> CreatePaymentAsync(PaymentCreationCommand command, CancellationToken cancellationToken)
    {
        // Momo amount is whole VND. requestId may equal orderId (both must be unique per order).
        var amount = (long)command.Amount;
        var orderId = command.OrderId;
        var requestId = orderId;
        const string extraData = "";

        var rawSignature =
            $"accessKey={_options.AccessKey}&amount={amount}&extraData={extraData}" +
            $"&ipnUrl={_options.IpnUrl}&orderId={orderId}&orderInfo={command.OrderInfo}" +
            $"&partnerCode={_options.PartnerCode}&redirectUrl={_options.RedirectUrl}" +
            $"&requestId={requestId}&requestType={RequestType}";

        var signature = HmacSha256Hex(rawSignature, _options.SecretKey);

        var body = new Dictionary<string, object?>
        {
            ["partnerCode"] = _options.PartnerCode,
            ["partnerName"] = "UNI-EDU",
            ["storeId"] = "UNIEDU",
            ["requestId"] = requestId,
            ["amount"] = amount,
            ["orderId"] = orderId,
            ["orderInfo"] = command.OrderInfo,
            ["redirectUrl"] = _options.RedirectUrl,
            ["ipnUrl"] = _options.IpnUrl,
            ["lang"] = "vi",
            ["requestType"] = RequestType,
            ["autoCapture"] = true,
            ["extraData"] = extraData,
            ["signature"] = signature
        };

        using var response = await _httpClient.PostAsJsonAsync(_options.CreateUrl, body, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<MomoCreateResponse>(cancellationToken: cancellationToken);

        if (result is null || result.ResultCode != 0 || string.IsNullOrWhiteSpace(result.PayUrl))
            throw new BadRequestException($"Momo payment creation failed: {result?.Message ?? "no response"} (code {result?.ResultCode}).");

        return new PaymentCreationResult(result.PayUrl);
    }

    public bool VerifyIpnSignature(MomoIpnCallback callback)
    {
        var rawSignature =
            $"accessKey={_options.AccessKey}&amount={callback.Amount}&extraData={callback.ExtraData}" +
            $"&message={callback.Message}&orderId={callback.OrderId}&orderInfo={callback.OrderInfo}" +
            $"&orderType={callback.OrderType}&partnerCode={callback.PartnerCode}&payType={callback.PayType}" +
            $"&requestId={callback.RequestId}&responseTime={callback.ResponseTime}" +
            $"&resultCode={callback.ResultCode}&transId={callback.TransId}";

        var expected = HmacSha256Hex(rawSignature, _options.SecretKey);

        // Constant-time compare; Momo emits lowercase hex.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes((callback.Signature ?? string.Empty).Trim().ToLowerInvariant()));
    }

    private static string HmacSha256Hex(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed record MomoCreateResponse(int ResultCode, string Message, string PayUrl);
}

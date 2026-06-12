namespace UNI_EDU_Backend.Infrastructure.Payments;

// PayOS merchant credentials + URLs (bound from the "PayOs" config section / env PayOs__*).
public class PayOsOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
    public string CreateUrl { get; set; } = "https://api-merchant.payos.vn/v2/payment-requests";
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

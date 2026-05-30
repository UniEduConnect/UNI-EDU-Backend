namespace UNI_EDU_Backend.Infrastructure.Payments;

// Bound from configuration section "VnPay" in Program.cs. Supply via .env / appsettings /
// environment for the sandbox merchant. The integration will not work until these are set.
public class VnPayOptions
{
    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;

    // VNPay sandbox payment page.
    public string PayUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

    // Frontend page VNPay redirects the browser back to after payment (UX only — never credits).
    // Note: VNPay also requires an IPN URL, but that is configured server-side in the VNPay
    // merchant dashboard (not sent per-request like Momo).
    public string ReturnUrl { get; set; } = string.Empty;
}

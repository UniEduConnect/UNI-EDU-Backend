namespace UNI_EDU_Backend.Infrastructure.Payments;

// Bound from configuration section "Momo" in Program.cs. Supply via .env / appsettings /
// environment for the sandbox merchant. The integration will not work until these are set.
public class MomoOptions
{
    public string PartnerCode { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    // Momo sandbox create endpoint.
    public string CreateUrl { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";

    // Frontend page Momo redirects the browser back to after payment (UX only — never credits).
    public string RedirectUrl { get; set; } = string.Empty;

    // Public URL of our momo-ipn endpoint. Locally this must be a tunnel (e.g. ngrok) so Momo
    // can reach it server-to-server.
    public string IpnUrl { get; set; } = string.Empty;
}

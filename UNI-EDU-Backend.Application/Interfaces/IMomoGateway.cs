using UNI_EDU_Backend.Application.DTOs.Wallets;

namespace UNI_EDU_Backend.Application.Interfaces;

// Momo-specific extension of the gateway port. The IPN payload + signature scheme are
// provider-specific, so verification lives behind its own method.
public interface IMomoGateway : IPaymentGateway
{
    // Recomputes the HMAC-SHA256 signature over the documented field set and compares it
    // to the value Momo sent. False ⇒ reject the callback (possible forgery).
    bool VerifyIpnSignature(MomoIpnCallback callback);
}

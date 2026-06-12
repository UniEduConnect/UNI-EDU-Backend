using UNI_EDU_Backend.Application.DTOs.Wallets;

namespace UNI_EDU_Backend.Application.Interfaces;

// PayOS-specific extension of the gateway port. The webhook signature is an HMAC-SHA256
// over the `data` object's alphabetically-sorted key=value form.
public interface IPayOsGateway : IPaymentGateway
{
    bool VerifyWebhookSignature(PayOsWebhookPayload payload);
}

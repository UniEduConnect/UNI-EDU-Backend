namespace UNI_EDU_Backend.Application.Interfaces;

// Port for a payment provider that hosts the payment page (redirect flow).
// Resolved by Method so new providers slot in without touching the service.
public interface IPaymentGateway
{
    // Lowercase provider key matching DepositRequest.Method ("momo", "vnpay", ...).
    string Method { get; }

    // Creates a payment order at the provider and returns the URL to redirect the user to.
    Task<PaymentCreationResult> CreatePaymentAsync(PaymentCreationCommand command, CancellationToken cancellationToken);
}

public record PaymentCreationCommand(decimal Amount, string OrderId, string OrderInfo);

public record PaymentCreationResult(string PayUrl);

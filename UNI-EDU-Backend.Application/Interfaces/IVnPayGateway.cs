namespace UNI_EDU_Backend.Application.Interfaces;

// VNPay-specific extension of the gateway port. VNPay's callback is a query-string with
// arbitrary vnp_* fields, so verification operates over a flat dictionary plus the provided
// vnp_SecureHash. Excludes vnp_SecureHash / vnp_SecureHashType from the dictionary.
public interface IVnPayGateway : IPaymentGateway
{
    bool VerifyCallbackSignature(IReadOnlyDictionary<string, string> vnpFields, string providedHash);
}

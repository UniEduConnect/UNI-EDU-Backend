namespace UNI_EDU_Backend.Application.DTOs.Wallets;

public class DepositRequest
{
    public decimal Amount { get; set; }

    // momo | vnpay | bank. Only providers with a registered IPaymentGateway are accepted.
    public string Method { get; set; } = string.Empty;
}

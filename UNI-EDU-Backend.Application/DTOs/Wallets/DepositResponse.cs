namespace UNI_EDU_Backend.Application.DTOs.Wallets;

public class DepositResponse
{
    public Guid TransactionId { get; set; }

    // Provider-hosted payment page the client should redirect the user to.
    public string PayUrl { get; set; } = string.Empty;

    // Always "pending" at this point — balance is credited only after the provider IPN confirms.
    public string Status { get; set; } = "pending";
}

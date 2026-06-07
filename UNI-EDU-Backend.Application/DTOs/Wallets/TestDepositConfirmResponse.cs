namespace UNI_EDU_Backend.Application.DTOs.Wallets;

// Response of POST /api/wallet/deposit-test/{transactionId}/confirm.
// Surfaces the post-credit balance so the UI does not need a second GET /wallet round-trip.
public class TestDepositConfirmResponse
{
    public Guid TransactionId { get; set; }
    public string Status { get; set; } = "completed";
    public decimal Balance { get; set; }
}

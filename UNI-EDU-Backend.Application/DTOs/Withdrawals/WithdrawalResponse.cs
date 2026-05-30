namespace UNI_EDU_Backend.Application.DTOs.Withdrawals;

// Mirrors the frontend WithdrawalRequest type minus the finance-only aggregates
// (tutorName/tutorAvatar/totalEarned/totalWithdrawn) — those belong on the finance
// list endpoint, not the create response.
public class WithdrawalResponse
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime RequestDate { get; set; }
}

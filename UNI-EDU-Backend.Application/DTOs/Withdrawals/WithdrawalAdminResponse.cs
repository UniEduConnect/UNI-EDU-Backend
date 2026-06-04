namespace UNI_EDU_Backend.Application.DTOs.Withdrawals;

// Finance-portal view of a withdrawal request, including tutor identity + lifetime aggregates.
public class WithdrawalAdminResponse
{
    public Guid Id { get; set; }
    public Guid TutorId { get; set; }
    public string TutorName { get; set; } = string.Empty;
    public string? TutorAvatar { get; set; }

    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string? Note { get; set; }

    // "pending" | "approved" | "rejected"
    public string Status { get; set; } = "pending";
    public DateTime RequestDate { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }

    // Lifetime totals for the tutor (approved payouts).
    public decimal TotalWithdrawn { get; set; }
}

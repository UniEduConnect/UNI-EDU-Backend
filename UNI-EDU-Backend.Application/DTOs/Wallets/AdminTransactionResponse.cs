namespace UNI_EDU_Backend.Application.DTOs.Wallets;

// System-wide ledger row for the finance portal (every user's transactions).
public class AdminTransactionResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string User { get; set; } = string.Empty;

    // "admin" | "tutor" | "student" | "parent"
    public string UserRole { get; set; } = string.Empty;

    // deposit | escrow_in | escrow_release | withdrawal | refund | platform_fee
    public string Type { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    // "pending" | "completed" | "failed"
    public string Status { get; set; } = "completed";

    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? ReceiptUrl { get; set; }
}

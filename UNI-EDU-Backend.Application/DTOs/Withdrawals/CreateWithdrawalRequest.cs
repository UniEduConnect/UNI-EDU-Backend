namespace UNI_EDU_Backend.Application.DTOs.Withdrawals;

public class CreateWithdrawalRequest
{
    public decimal Amount { get; set; }

    // Only "bank" is supported for now (validator rejects others).
    public string Method { get; set; } = string.Empty;

    public string? BankAccount { get; set; }
    public string? BankName { get; set; }

    public string? Note { get; set; }
}

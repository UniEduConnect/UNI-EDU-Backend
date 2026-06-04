namespace UNI_EDU_Backend.Application.DTOs.Withdrawals;

public class WithdrawalListQuery
{
    // "pending" | "approved" | "rejected". Omit for all.
    public string? Status { get; set; }

    // 1-based. Default page size is 10 (see FinanceService.PageSize).
    public int Page { get; set; } = 1;
}

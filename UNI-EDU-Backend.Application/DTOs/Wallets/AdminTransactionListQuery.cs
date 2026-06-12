namespace UNI_EDU_Backend.Application.DTOs.Wallets;

public class AdminTransactionListQuery
{
    // deposit | escrow_in | escrow_release | withdrawal | refund | platform_fee. Omit for all.
    public string? Type { get; set; }

    // "pending" | "completed" | "failed". Omit for all.
    public string? Status { get; set; }

    // 1-based. Default page size is 20 (see FinanceService.TransactionPageSize).
    public int Page { get; set; } = 1;
}

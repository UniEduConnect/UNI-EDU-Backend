namespace UNI_EDU_Backend.Application.DTOs.Wallets;

public class TransactionListQuery
{
    // 1-based. Server page size is fixed at 10 (see WalletService.PageSize).
    public int Page { get; set; } = 1;
}

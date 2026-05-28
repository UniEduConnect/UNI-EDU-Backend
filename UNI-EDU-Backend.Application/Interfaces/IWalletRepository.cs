using UNI_EDU_Backend.Application.DTOs.Wallets;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories
{
    public interface IWalletRepository
    {
        // Spendable balance for the user, or 0 if they have no wallet row yet.
        Task<decimal> GetBalanceAsync(Guid userId, CancellationToken cancellationToken);

        // Pending earnings for a tutor = Σ(EscrowAmount - EscrowReleased) across their classes.
        // Computed from Classes — NOT the Wallet.EscrowBalance column (that tracks the payer side).
        Task<decimal> GetTutorOutstandingEscrowAsync(Guid tutorId, CancellationToken cancellationToken);

        // Paged ledger for the user, newest first. ChildId is the related class's student
        // (for the parent view); null when the transaction has no related class.
        Task<(List<WalletTransactionRow> Items, int Total)> GetTransactionsAsync(
            Guid userId, int page, int pageSize, CancellationToken cancellationToken);
    }
}

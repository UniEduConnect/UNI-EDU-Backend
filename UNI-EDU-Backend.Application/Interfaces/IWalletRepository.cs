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

        // Creates the Wallet row if missing (balance 0), then inserts a Pending deposit
        // transaction. Does NOT credit the balance. Returns the new transaction id.
        Task<Guid> CreatePendingDepositAsync(
            Guid userId, decimal amount, string method, string orderId, string description, CancellationToken cancellationToken);

        // Idempotently applies a provider result to the pending deposit identified by orderId.
        // On success (and matching amount) flips Pending→Completed and credits the balance,
        // all inside one DB transaction. Safe to call repeatedly (webhook retries).
        Task<DepositSettleOutcome> SettleDepositAsync(
            string orderId, bool success, string providerTxnId, decimal confirmedAmount, CancellationToken cancellationToken);

        // Tiny lookup used by the mock-confirm flow (POST /wallet/deposit-test/{id}/confirm).
        // Returns null if the transaction id doesn't exist. The service uses this to enforce
        // ownership + "must still be pending" + "must be a test row" before calling SettleDepositAsync.
        Task<TestDepositLookup?> LookupTestDepositAsync(Guid transactionId, CancellationToken cancellationToken);
    }
}

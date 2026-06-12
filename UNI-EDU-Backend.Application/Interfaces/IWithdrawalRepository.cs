using UNI_EDU_Backend.Application.DTOs.Withdrawals;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories
{
    public interface IWithdrawalRepository
    {
        // Atomic: verify wallet balance, debit it (lock funds), insert a Pending withdrawal row.
        // Throws BadRequestException if the wallet is missing or balance < amount.
        // A future approve endpoint flips Pending→Approved (paid); a reject endpoint refunds.
        Task<WithdrawalResponse> CreateAsync(Guid tutorId, CreateWithdrawalRequest request, CancellationToken cancellationToken);

        // Finance portal: paged list, optionally filtered by status.
        Task<(List<WithdrawalAdminResponse> Items, int Total)> GetAllAsync(
            UNI_EDU_Backend.Domain.Enums.WithdrawalStatus? status, int page, int pageSize, CancellationToken cancellationToken);

        // Pending→Approved: records the payout in the ledger (balance was already debited at create).
        Task<WithdrawalReviewResult> ApproveAsync(Guid withdrawalId, Guid reviewerId, string? note, CancellationToken cancellationToken);

        // Pending→Rejected: refunds the locked amount back to the tutor's spendable balance.
        Task<WithdrawalReviewResult> RejectAsync(Guid withdrawalId, Guid reviewerId, string? note, CancellationToken cancellationToken);
    }
}

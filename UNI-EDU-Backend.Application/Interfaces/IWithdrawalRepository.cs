using UNI_EDU_Backend.Application.DTOs.Withdrawals;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories
{
    public interface IWithdrawalRepository
    {
        // Atomic: verify wallet balance, debit it (lock funds), insert a Pending withdrawal row.
        // Throws BadRequestException if the wallet is missing or balance < amount.
        // A future approve endpoint flips Pending→Approved (paid); a reject endpoint refunds.
        Task<WithdrawalResponse> CreateAsync(Guid tutorId, CreateWithdrawalRequest request, CancellationToken cancellationToken);
    }
}

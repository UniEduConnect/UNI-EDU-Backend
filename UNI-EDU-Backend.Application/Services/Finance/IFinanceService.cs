using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Wallets;
using UNI_EDU_Backend.Application.DTOs.Withdrawals;

namespace UNI_EDU_Backend.Application.Services.Finance;

public interface IFinanceService
{
    Task<PagedResult<WithdrawalAdminResponse>> GetWithdrawalsAsync(WithdrawalListQuery query, CancellationToken cancellationToken);
    Task ApproveWithdrawalAsync(Guid withdrawalId, ReviewWithdrawalRequest request, Guid reviewerId, CancellationToken cancellationToken);
    Task RejectWithdrawalAsync(Guid withdrawalId, ReviewWithdrawalRequest request, Guid reviewerId, CancellationToken cancellationToken);

    Task<PagedResult<AdminTransactionResponse>> GetTransactionsAsync(AdminTransactionListQuery query, CancellationToken cancellationToken);
}

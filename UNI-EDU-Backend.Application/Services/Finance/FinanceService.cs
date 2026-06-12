using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Wallets;
using UNI_EDU_Backend.Application.DTOs.Withdrawals;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Services.Finance;

public class FinanceService(
    IWithdrawalRepository withdrawalRepo,
    IWalletRepository walletRepo,
    IAdminRepository adminRepo) : IFinanceService
{
    private const int PageSize = 10;
    private const int TransactionPageSize = 20;

    private readonly IWithdrawalRepository _withdrawalRepo = withdrawalRepo;
    private readonly IWalletRepository _walletRepo = walletRepo;
    private readonly IAdminRepository _adminRepo = adminRepo;

    public async Task<PagedResult<WithdrawalAdminResponse>> GetWithdrawalsAsync(WithdrawalListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var status = ParseStatus(query.Status);

        var (items, total) = await _withdrawalRepo.GetAllAsync(status, page, PageSize, cancellationToken);

        return new PagedResult<WithdrawalAdminResponse>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = PageSize
        };
    }

    public async Task ApproveWithdrawalAsync(Guid withdrawalId, ReviewWithdrawalRequest request, Guid reviewerId, CancellationToken cancellationToken)
    {
        var result = await _withdrawalRepo.ApproveAsync(withdrawalId, reviewerId, request.Note, cancellationToken);
        EnsureReviewable(result.Outcome, withdrawalId);

        await _adminRepo.AddAuditLogAsync(reviewerId, "Duyệt rút tiền",
            $"{result.TutorName} — {result.Amount:N0} VND", cancellationToken);
    }

    public async Task RejectWithdrawalAsync(Guid withdrawalId, ReviewWithdrawalRequest request, Guid reviewerId, CancellationToken cancellationToken)
    {
        var result = await _withdrawalRepo.RejectAsync(withdrawalId, reviewerId, request.Note, cancellationToken);
        EnsureReviewable(result.Outcome, withdrawalId);

        await _adminRepo.AddAuditLogAsync(reviewerId, "Từ chối rút tiền",
            $"{result.TutorName} — {result.Amount:N0} VND", cancellationToken);
    }

    public async Task<PagedResult<AdminTransactionResponse>> GetTransactionsAsync(AdminTransactionListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;

        var (items, total) = await _walletRepo.GetAllTransactionsAsync(
            query.Type?.Trim().ToLowerInvariant(),
            query.Status?.Trim().ToLowerInvariant(),
            page, TransactionPageSize, cancellationToken);

        return new PagedResult<AdminTransactionResponse>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = TransactionPageSize
        };
    }

    private static void EnsureReviewable(WithdrawalReviewOutcome outcome, Guid withdrawalId)
    {
        switch (outcome)
        {
            case WithdrawalReviewOutcome.NotFound:
                throw new NotFoundException($"Withdrawal with id '{withdrawalId}' not found.");
            case WithdrawalReviewOutcome.AlreadyReviewed:
                throw new BadRequestException("This withdrawal has already been reviewed.");
        }
    }

    private static WithdrawalStatus? ParseStatus(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "pending" => WithdrawalStatus.Pending,
            "approved" => WithdrawalStatus.Approved,
            "rejected" => WithdrawalStatus.Rejected,
            _ => null
        };
}

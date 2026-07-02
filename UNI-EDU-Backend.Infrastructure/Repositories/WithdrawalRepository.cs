using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Withdrawals;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class WithdrawalRepository(ApplicationDbContext dbContext) : IWithdrawalRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<WithdrawalResponse> CreateAsync(Guid tutorId, CreateWithdrawalRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var method = request.Method.Trim().ToLowerInvariant();

        // One transaction: read wallet → check balance → debit → insert row → commit.
        // Prevents double-spend if a tutor races two withdraw calls.
        await using var dbTx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserID == tutorId, cancellationToken)
            ?? throw new BadRequestException("Wallet not found for this tutor.");

        if (wallet.Balance < request.Amount)
            throw new BadRequestException(
                $"Insufficient balance. Requested: {request.Amount:N0} VND, available: {wallet.Balance:N0} VND.");

        // Lock funds by debiting now; the reject endpoint will refund this on rejection.
        wallet.Balance -= request.Amount;
        wallet.UpdatedAt = now;

        var withdrawal = new Withdrawal
        {
            WithdrawalID = Guid.NewGuid(),
            TutorID = tutorId,
            Amount = request.Amount,
            Method = method,
            BankAccount = request.BankAccount,
            BankName = request.BankName,
            Note = request.Note,
            Status = WithdrawalStatus.Pending,
            RequestedAt = now
        };

        // Mirror the request in the ledger (pending, negative = outflow) so it shows in
        // the tutor's transaction history immediately. Approve/reject updates this row's status.
        var ledgerTx = new WalletTransaction
        {
            TransactionID = Guid.NewGuid(),
            UserID = tutorId,
            Type = WalletTxType.Withdrawal,
            Amount = -request.Amount,
            Status = WalletTxStatus.Pending,
            Method = method,
            Description = $"Rút tiền về {request.BankName} ({request.BankAccount})",
            CreatedAt = now
        };
        withdrawal.LedgerTransactionId = ledgerTx.TransactionID;

        _dbContext.Withdrawals.Add(withdrawal);
        _dbContext.WalletTransactions.Add(ledgerTx);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await dbTx.CommitAsync(cancellationToken);

        return new WithdrawalResponse
        {
            Id = withdrawal.WithdrawalID,
            Amount = withdrawal.Amount,
            Method = withdrawal.Method,
            BankName = withdrawal.BankName ?? string.Empty,
            BankAccount = withdrawal.BankAccount ?? string.Empty,
            Note = withdrawal.Note,
            Status = "pending",
            RequestDate = withdrawal.RequestedAt
        };
    }

    public async Task<(List<WithdrawalAdminResponse> Items, int Total)> GetAllAsync(WithdrawalStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var q = _dbContext.Withdrawals.AsNoTracking();
        if (status is not null)
            q = q.Where(w => w.Status == status);

        var total = await q.CountAsync(cancellationToken);

        var rows = await q
            .OrderByDescending(w => w.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new
            {
                w.WithdrawalID,
                w.TutorID,
                TutorName = w.Tutor.FullName ?? w.Tutor.User.Fullname,
                TutorAvatar = w.Tutor.AvatarUrl,
                w.Amount,
                w.Method,
                w.BankName,
                w.BankAccount,
                w.Note,
                w.Status,
                w.RequestedAt,
                w.ReviewedAt,
                w.ReviewNote,
                TotalWithdrawn = _dbContext.Withdrawals
                    .Where(x => x.TutorID == w.TutorID && x.Status == WithdrawalStatus.Approved)
                    .Sum(x => (decimal?)x.Amount) ?? 0m
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(w => new WithdrawalAdminResponse
        {
            Id = w.WithdrawalID,
            TutorId = w.TutorID,
            TutorName = w.TutorName ?? string.Empty,
            TutorAvatar = w.TutorAvatar,
            Amount = w.Amount,
            Method = w.Method,
            BankName = w.BankName ?? string.Empty,
            BankAccount = w.BankAccount ?? string.Empty,
            Note = w.Note,
            Status = w.Status.ToString().ToLowerInvariant(),
            RequestDate = w.RequestedAt,
            ReviewedAt = w.ReviewedAt,
            ReviewNote = w.ReviewNote,
            TotalWithdrawn = w.TotalWithdrawn
        }).ToList();

        return (items, total);
    }

    public async Task<WithdrawalReviewResult> ApproveAsync(Guid withdrawalId, Guid reviewerId, string? note, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var withdrawal = await _dbContext.Withdrawals.FirstOrDefaultAsync(w => w.WithdrawalID == withdrawalId, cancellationToken);
        if (withdrawal is null)
            return new WithdrawalReviewResult(WithdrawalReviewOutcome.NotFound, string.Empty, 0m);

        var tutorName = await TutorNameAsync(withdrawal.TutorID, cancellationToken);

        if (withdrawal.Status != WithdrawalStatus.Pending)
            return new WithdrawalReviewResult(WithdrawalReviewOutcome.AlreadyReviewed, tutorName, withdrawal.Amount);

        withdrawal.Status = WithdrawalStatus.Approved;
        withdrawal.ReviewedAt = now;
        withdrawal.ReviewerID = reviewerId;
        withdrawal.ReviewNote = note;

        // Funds were already debited at request time — settle the pending ledger row.
        var ledger = withdrawal.LedgerTransactionId is Guid ledgerId
            ? await _dbContext.WalletTransactions.FirstOrDefaultAsync(t => t.TransactionID == ledgerId, cancellationToken)
            : null;
        if (ledger is not null)
        {
            ledger.Status = WalletTxStatus.Completed;
        }
        else
        {
            // Legacy withdrawals created before the ledger link — insert a completed payout row.
            _dbContext.WalletTransactions.Add(new WalletTransaction
            {
                TransactionID = Guid.NewGuid(),
                UserID = withdrawal.TutorID,
                Type = WalletTxType.Withdrawal,
                Amount = -withdrawal.Amount,
                Status = WalletTxStatus.Completed,
                Method = withdrawal.Method,
                Description = $"Rút tiền về {withdrawal.BankName} ({withdrawal.BankAccount})",
                CreatedAt = now
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new WithdrawalReviewResult(WithdrawalReviewOutcome.Done, tutorName, withdrawal.Amount);
    }

    public async Task<WithdrawalReviewResult> RejectAsync(Guid withdrawalId, Guid reviewerId, string? note, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var withdrawal = await _dbContext.Withdrawals.FirstOrDefaultAsync(w => w.WithdrawalID == withdrawalId, cancellationToken);
        if (withdrawal is null)
            return new WithdrawalReviewResult(WithdrawalReviewOutcome.NotFound, string.Empty, 0m);

        var tutorName = await TutorNameAsync(withdrawal.TutorID, cancellationToken);

        if (withdrawal.Status != WithdrawalStatus.Pending)
            return new WithdrawalReviewResult(WithdrawalReviewOutcome.AlreadyReviewed, tutorName, withdrawal.Amount);

        withdrawal.Status = WithdrawalStatus.Rejected;
        withdrawal.ReviewedAt = now;
        withdrawal.ReviewerID = reviewerId;
        withdrawal.ReviewNote = note;

        // Refund the locked amount back to the tutor's spendable balance.
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserID == withdrawal.TutorID, cancellationToken);
        if (wallet is null)
        {
            wallet = new Wallet { UserID = withdrawal.TutorID, Balance = 0m, EscrowBalance = 0m, UpdatedAt = now };
            _dbContext.Wallets.Add(wallet);
        }
        wallet.Balance += withdrawal.Amount;
        wallet.UpdatedAt = now;

        // Mark the pending withdrawal ledger row as failed…
        if (withdrawal.LedgerTransactionId is Guid ledgerId)
        {
            var ledger = await _dbContext.WalletTransactions.FirstOrDefaultAsync(t => t.TransactionID == ledgerId, cancellationToken);
            if (ledger is not null) ledger.Status = WalletTxStatus.Failed;
        }

        // …and record the refund of the locked amount back to the wallet.
        _dbContext.WalletTransactions.Add(new WalletTransaction
        {
            TransactionID = Guid.NewGuid(),
            UserID = withdrawal.TutorID,
            Type = WalletTxType.Refund,
            Amount = withdrawal.Amount,
            Status = WalletTxStatus.Completed,
            Description = "Hoàn tiền do yêu cầu rút bị từ chối",
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new WithdrawalReviewResult(WithdrawalReviewOutcome.Done, tutorName, withdrawal.Amount);
    }

    private Task<string> TutorNameAsync(Guid tutorId, CancellationToken cancellationToken) =>
        _dbContext.Tutors
            .Where(t => t.TutorID == tutorId)
            .Select(t => t.FullName ?? t.User.Fullname)
            .FirstOrDefaultAsync(cancellationToken)!;
}

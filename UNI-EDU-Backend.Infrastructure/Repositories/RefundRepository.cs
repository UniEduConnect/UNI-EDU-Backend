using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Refunds;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class RefundRepository(ApplicationDbContext dbContext) : IRefundRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public Task<ClassRefundContext?> GetClassForRefundAsync(Guid classId, CancellationToken cancellationToken) =>
        _dbContext.Classes.AsNoTracking()
            .Where(c => c.ClassID == classId)
            .Select(c => new ClassRefundContext(c.TutorID, c.StudentID, c.EscrowAmount - c.EscrowReleased))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> IsParentOfStudentAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken) =>
        _dbContext.Students.AnyAsync(s => s.StudentID == studentId && s.ParentID == parentId, cancellationToken);

    public async Task<RefundResponse> CreateAsync(Guid classId, Guid requesterId, decimal amount, decimal maxAmount, string reason, CancellationToken cancellationToken)
    {
        var entity = new RefundRequest
        {
            RefundRequestID = Guid.NewGuid(),
            ClassID = classId,
            RequesterUserId = requesterId,
            Amount = amount,
            MaxAmount = maxAmount,
            Reason = reason,
            Status = RefundStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.RefundRequests.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ByIdAsync(entity.RefundRequestID, cancellationToken);
    }

    public async Task<(List<RefundResponse> Items, int Total)> GetAllAsync(RefundStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var q = _dbContext.RefundRequests.AsNoTracking();
        if (status is not null) q = q.Where(r => r.Status == status);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(List<RefundResponse> Items, int Total)> GetForTutorAsync(Guid tutorId, RefundStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var q = _dbContext.RefundRequests.AsNoTracking().Where(r => r.Class.TutorID == tutorId);
        if (status is not null) q = q.Where(r => r.Status == status);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<RefundReviewResult> ApproveAsync(Guid refundId, Guid reviewerId, string? note, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var refund = await _dbContext.RefundRequests.FirstOrDefaultAsync(r => r.RefundRequestID == refundId, cancellationToken);
        if (refund is null) return new RefundReviewResult(RefundReviewOutcome.NotFound, string.Empty, 0m);

        var classRow = await _dbContext.Classes.FirstOrDefaultAsync(c => c.ClassID == refund.ClassID, cancellationToken);
        var studentName = classRow is null ? string.Empty : await StudentNameAsync(classRow.StudentID, cancellationToken);

        if (refund.Status != RefundStatus.Pending)
            return new RefundReviewResult(RefundReviewOutcome.AlreadyReviewed, studentName, refund.Amount);

        if (classRow is not null)
        {
            // Cap to whatever escrow is still held.
            var remaining = classRow.EscrowAmount - classRow.EscrowReleased;
            var amount = Math.Min(refund.Amount, remaining);
            if (amount < 0m) amount = 0m;

            if (amount > 0m)
            {
                classRow.EscrowReleased += amount;

                var studentWallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserID == classRow.StudentID, cancellationToken);
                if (studentWallet is not null)
                {
                    studentWallet.EscrowBalance -= amount;
                    studentWallet.Balance += amount;
                    studentWallet.UpdatedAt = now;
                }

                _dbContext.WalletTransactions.Add(new WalletTransaction
                {
                    TransactionID = Guid.NewGuid(),
                    UserID = classRow.StudentID,
                    Type = WalletTxType.Refund,
                    Amount = amount,
                    Status = WalletTxStatus.Completed,
                    RelatedClassID = classRow.ClassID,
                    Description = $"Refund for class {classRow.Name}",
                    CreatedAt = now
                });
            }
        }

        refund.Status = RefundStatus.Approved;
        refund.ReviewedAt = now;
        refund.ReviewerID = reviewerId;
        refund.ReviewNote = note;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new RefundReviewResult(RefundReviewOutcome.Done, studentName, refund.Amount);
    }

    public async Task<RefundReviewResult> RejectAsync(Guid refundId, Guid reviewerId, string? note, CancellationToken cancellationToken)
    {
        var refund = await _dbContext.RefundRequests.FirstOrDefaultAsync(r => r.RefundRequestID == refundId, cancellationToken);
        if (refund is null) return new RefundReviewResult(RefundReviewOutcome.NotFound, string.Empty, 0m);

        var classRow = await _dbContext.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.ClassID == refund.ClassID, cancellationToken);
        var studentName = classRow is null ? string.Empty : await StudentNameAsync(classRow.StudentID, cancellationToken);

        if (refund.Status != RefundStatus.Pending)
            return new RefundReviewResult(RefundReviewOutcome.AlreadyReviewed, studentName, refund.Amount);

        refund.Status = RefundStatus.Rejected;
        refund.ReviewedAt = DateTime.UtcNow;
        refund.ReviewerID = reviewerId;
        refund.ReviewNote = note;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new RefundReviewResult(RefundReviewOutcome.Done, studentName, refund.Amount);
    }

    public Task<int> CountPendingAsync(CancellationToken cancellationToken) =>
        _dbContext.RefundRequests.CountAsync(r => r.Status == RefundStatus.Pending, cancellationToken);

    private Task<string> StudentNameAsync(Guid studentId, CancellationToken cancellationToken) =>
        _dbContext.Students.Where(s => s.StudentID == studentId)
            .Select(s => s.FullName ?? s.User.Fullname)
            .FirstOrDefaultAsync(cancellationToken)!;

    private Task<RefundResponse> ByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.RefundRequests.AsNoTracking()
            .Where(r => r.RefundRequestID == id)
            .Select(Projection)
            .FirstAsync(cancellationToken);

    private static readonly System.Linq.Expressions.Expression<Func<RefundRequest, RefundResponse>> Projection =
        r => new RefundResponse
        {
            Id = r.RefundRequestID,
            ClassId = r.ClassID,
            ClassName = r.Class.Name,
            TutorName = r.Class.Tutor.FullName ?? r.Class.Tutor.User.Fullname,
            StudentName = r.Class.Student.FullName ?? r.Class.Student.User.Fullname,
            Amount = r.Amount,
            MaxAmount = r.MaxAmount,
            Reason = r.Reason,
            Status = r.Status == RefundStatus.Approved ? "approved" : r.Status == RefundStatus.Rejected ? "rejected" : "pending",
            CreatedAt = r.CreatedAt,
            ReviewedAt = r.ReviewedAt,
            ReviewNote = r.ReviewNote
        };
}

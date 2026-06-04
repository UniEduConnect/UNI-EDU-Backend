using UNI_EDU_Backend.Application.DTOs.Refunds;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

// Class context needed to authorize + size a refund request.
public record ClassRefundContext(Guid TutorId, Guid StudentId, decimal RemainingEscrow);

public interface IRefundRepository
{
    Task<ClassRefundContext?> GetClassForRefundAsync(Guid classId, CancellationToken cancellationToken);
    Task<bool> IsParentOfStudentAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken);

    Task<RefundResponse> CreateAsync(Guid classId, Guid requesterId, decimal amount, decimal maxAmount, string reason, CancellationToken cancellationToken);

    Task<(List<RefundResponse> Items, int Total)> GetAllAsync(RefundStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    // Approve: move the refunded amount from the student's held escrow back to spendable balance.
    Task<RefundReviewResult> ApproveAsync(Guid refundId, Guid reviewerId, string? note, CancellationToken cancellationToken);
    Task<RefundReviewResult> RejectAsync(Guid refundId, Guid reviewerId, string? note, CancellationToken cancellationToken);

    Task<int> CountPendingAsync(CancellationToken cancellationToken);
}

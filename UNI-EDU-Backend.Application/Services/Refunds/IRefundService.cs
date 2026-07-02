using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Refunds;

namespace UNI_EDU_Backend.Application.Services.Refunds;

public interface IRefundService
{
    Task<RefundResponse> CreateAsync(Guid classId, CreateRefundRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task<PagedResult<RefundResponse>> GetAllAsync(RefundListQuery query, CancellationToken cancellationToken);
    Task<PagedResult<RefundResponse>> GetForTutorAsync(Guid tutorId, RefundListQuery query, CancellationToken cancellationToken);
    Task ApproveAsync(Guid refundId, ReviewRefundRequest request, Guid reviewerId, CancellationToken cancellationToken);
    Task RejectAsync(Guid refundId, ReviewRefundRequest request, Guid reviewerId, CancellationToken cancellationToken);
}

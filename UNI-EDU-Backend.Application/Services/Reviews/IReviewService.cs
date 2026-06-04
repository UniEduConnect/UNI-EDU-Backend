using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Reviews;

namespace UNI_EDU_Backend.Application.Services.Reviews;

public interface IReviewService
{
    Task<ReviewResponse> CreateClassReviewAsync(Guid classId, CreateReviewRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task<PagedResult<MyReviewResponse>> GetMyReviewsAsync(Guid reviewerId, int page, CancellationToken cancellationToken);

    Task<PagedResult<ModerationReviewResponse>> GetForModerationAsync(ReviewModerationListQuery query, CancellationToken cancellationToken);
    Task SetHiddenAsync(int reviewId, bool hidden, CancellationToken cancellationToken);
}

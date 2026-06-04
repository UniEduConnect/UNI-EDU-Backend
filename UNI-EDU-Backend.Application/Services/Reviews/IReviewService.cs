using UNI_EDU_Backend.Application.DTOs.Reviews;

namespace UNI_EDU_Backend.Application.Services.Reviews;

public interface IReviewService
{
    Task<ReviewResponse> CreateClassReviewAsync(Guid classId, CreateReviewRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
}

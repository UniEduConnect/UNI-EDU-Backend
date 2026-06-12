using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Exams;
using UNI_EDU_Backend.Application.DTOs.Parents;

namespace UNI_EDU_Backend.Application.Services.Parents;

public interface IParentService
{
    Task<List<StudentSummaryResponse>> GetMyChildrenAsync(Guid parentId, CancellationToken cancellationToken);

    // A child's exam submissions (the caller must be the child's parent).
    Task<PagedResult<SubmissionResponse>> GetChildExamsAsync(Guid parentId, Guid childId, int page, CancellationToken cancellationToken);

    // A child's monthly progress + per-subject scores (caller must be the child's parent).
    Task<ChildProgressAnalyticsResponse> GetChildProgressAsync(Guid parentId, Guid childId, CancellationToken cancellationToken);

    // Transfer money from the parent's wallet into a child's wallet (to fund tuition/escrow).
    Task FundChildAsync(Guid parentId, Guid childId, decimal amount, CancellationToken cancellationToken);
}

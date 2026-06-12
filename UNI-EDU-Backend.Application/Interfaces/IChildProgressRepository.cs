using UNI_EDU_Backend.Application.DTOs.Parents;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IChildProgressRepository
{
    // Monthly progress + per-subject scores for a child, from real sessions + submissions.
    Task<ChildProgressAnalyticsResponse> GetChildProgressAsync(Guid childId, CancellationToken cancellationToken);
}

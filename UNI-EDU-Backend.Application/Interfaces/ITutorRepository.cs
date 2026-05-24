using UNI_EDU_Backend.Application.DTOs.Tutors;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Interfaces;

public interface ITutorRepository : IGenericRepository<Tutor>
{
    Task<(List<TutorListingResponse> Items, int Total)> SearchAsync(
        TutorSearchQuery query,
        int pageSize,
        CancellationToken cancellationToken);

    Task<TutorProfileResponse?> GetProfileByIdAsync(Guid tutorId, int recentReviewCount, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid tutorId, CancellationToken cancellationToken);

    Task<(List<TutorReviewResponse> Items, int Total)> GetReviewsByTutorIdAsync(Guid tutorId, int page, int pageSize, CancellationToken cancellationToken);
}

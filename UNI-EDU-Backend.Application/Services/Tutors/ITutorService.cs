using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Tutors;

namespace UNI_EDU_Backend.Application.Services.Tutors;

public interface ITutorService
{
    public Task<PagedResult<TutorListingResponse>> SearchTutorsAsync(TutorSearchQuery query, CancellationToken cancellationToken);

    public Task<TutorProfileResponse> GetTutorProfileAsync(Guid tutorId, CancellationToken cancellationToken);

    public Task<PagedResult<TutorReviewResponse>> GetTutorReviewsAsync(Guid tutorId, TutorReviewsQuery query, CancellationToken cancellationToken);
}

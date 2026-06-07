using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Tutors;

namespace UNI_EDU_Backend.Application.Services.Tutors;

public interface ITutorService
{
    public Task<PagedResult<TutorListingResponse>> SearchTutorsAsync(TutorSearchQuery query, CancellationToken cancellationToken);

    public Task<TutorProfileResponse> GetTutorProfileAsync(Guid tutorId, CancellationToken cancellationToken);

    public Task<PagedResult<TutorReviewResponse>> GetTutorReviewsAsync(Guid tutorId, TutorReviewsQuery query, CancellationToken cancellationToken);

    // Tutor-only (enforced at controller via [Authorize(Roles = "Tutor")]).
    // Get returns null when nothing is saved. Save upserts. Delete is a no-op if nothing saved.
    public Task<BankAccountResponse?> GetMyBankAccountAsync(Guid tutorId, CancellationToken cancellationToken);
    public Task<BankAccountResponse> SaveMyBankAccountAsync(Guid tutorId, SaveBankAccountRequest request, CancellationToken cancellationToken);
    public Task DeleteMyBankAccountAsync(Guid tutorId, CancellationToken cancellationToken);
}

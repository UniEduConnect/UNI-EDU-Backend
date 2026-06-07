using UNI_EDU_Backend.Application.DTOs.Tutors;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface ITutorRepository : IGenericRepository<Tutor>
{
    Task<(List<TutorListingResponse> Items, int Total)> SearchAsync(
        TutorSearchQuery query,
        int pageSize,
        CancellationToken cancellationToken);

    Task<TutorProfileResponse?> GetProfileByIdAsync(Guid tutorId, int recentReviewCount, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid tutorId, CancellationToken cancellationToken);

    Task<(List<TutorReviewResponse> Items, int Total)> GetReviewsByTutorIdAsync(Guid tutorId, int page, int pageSize, CancellationToken cancellationToken);

    // Saved bank account (single per tutor). Get returns null if none saved.
    // Save is upsert (writes all three columns). Delete clears all three. Returns false
    // from Save/Delete if the Tutor row itself doesn't exist (shouldn't happen for a real caller).
    Task<BankAccountResponse?> GetBankAccountAsync(Guid tutorId, CancellationToken cancellationToken);
    Task<bool> SaveBankAccountAsync(Guid tutorId, SaveBankAccountRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteBankAccountAsync(Guid tutorId, CancellationToken cancellationToken);
}

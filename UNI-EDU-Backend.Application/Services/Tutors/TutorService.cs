using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Tutors;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces;

namespace UNI_EDU_Backend.Application.Services.Tutors;

public class TutorService(ITutorRepository tutorRepo) : ITutorService
{
    private const int PageSize = 10;
    private const int RecentReviewCount = 10;

    private readonly ITutorRepository _tutorRepo = tutorRepo;

    public async Task<PagedResult<TutorListingResponse>> SearchTutorsAsync(TutorSearchQuery query, CancellationToken cancellationToken)
    {
        var (items, total) = await _tutorRepo.SearchAsync(query, PageSize, cancellationToken);

        return new PagedResult<TutorListingResponse>
        {
            Items = items,
            Total = total,
            Page = query.Page,
            PageSize = PageSize
        };
    }

    public async Task<TutorProfileResponse> GetTutorProfileAsync(Guid tutorId, CancellationToken cancellationToken)
    {
        var profile = await _tutorRepo.GetProfileByIdAsync(tutorId, RecentReviewCount, cancellationToken)
            ?? throw new NotFoundException($"Tutor with id '{tutorId}' not found.");

        return profile;
    }
}

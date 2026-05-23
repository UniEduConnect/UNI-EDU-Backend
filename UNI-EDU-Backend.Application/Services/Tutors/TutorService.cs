using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Tutors;
using UNI_EDU_Backend.Application.Interfaces;

namespace UNI_EDU_Backend.Application.Services.Tutors;

public class TutorService(ITutorRepository tutorRepo) : ITutorService
{
    private const int PageSize = 10;

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
}

using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Tutors;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces;

namespace UNI_EDU_Backend.Application.Services.Tutors;

public class TutorService(
    ITutorRepository tutorRepo,
    IValidator<TutorSearchQuery> searchValidator,
    IValidator<TutorReviewsQuery> reviewsValidator) : ITutorService
{
    private const int PageSize = 10;
    private const int RecentReviewCount = 10;

    private readonly ITutorRepository _tutorRepo = tutorRepo;
    private readonly IValidator<TutorSearchQuery> _searchValidator = searchValidator;
    private readonly IValidator<TutorReviewsQuery> _reviewsValidator = reviewsValidator;

    public async Task<PagedResult<TutorListingResponse>> SearchTutorsAsync(TutorSearchQuery query, CancellationToken cancellationToken)
    {
        await _searchValidator.EnsureValidAsync(query, cancellationToken);

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

    public async Task<PagedResult<TutorReviewResponse>> GetTutorReviewsAsync(Guid tutorId, TutorReviewsQuery query, CancellationToken cancellationToken)
    {
        await _reviewsValidator.EnsureValidAsync(query, cancellationToken);

        if (!await _tutorRepo.ExistsAsync(tutorId, cancellationToken))
            throw new NotFoundException($"Tutor with id '{tutorId}' not found.");

        var (items, total) = await _tutorRepo.GetReviewsByTutorIdAsync(tutorId, query.Page, PageSize, cancellationToken);

        return new PagedResult<TutorReviewResponse>
        {
            Items = items,
            Total = total,
            Page = query.Page,
            PageSize = PageSize
        };
    }
}

using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Reviews;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Reviews;

public class ReviewService(
    IReviewRepository reviewRepo,
    IValidator<CreateReviewRequest> createValidator) : IReviewService
{
    private readonly IReviewRepository _reviewRepo = reviewRepo;
    private readonly IValidator<CreateReviewRequest> _createValidator = createValidator;

    public async Task<ReviewResponse> CreateClassReviewAsync(Guid classId, CreateReviewRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        var ctx = await _reviewRepo.GetClassForReviewAsync(classId, cancellationToken)
            ?? throw new NotFoundException($"Class with id '{classId}' not found.");

        // Only the booking student or their parent can review the class's tutor.
        var role = (callerRole ?? string.Empty).Trim();
        bool allowed = role switch
        {
            "Student" => ctx.StudentId == callerUserId,
            "Parent" => await _reviewRepo.IsParentOfStudentAsync(callerUserId, ctx.StudentId, cancellationToken),
            _ => false
        };

        if (!allowed)
            throw new ForbiddenAccessException("Only the student or their parent can review this class.");

        if (await _reviewRepo.HasReviewedAsync(classId, callerUserId, cancellationToken))
            throw new BadRequestException("You have already reviewed this class.");

        return await _reviewRepo.CreateAsync(classId, ctx.TutorId, callerUserId, request.Rating, request.Comment, cancellationToken);
    }

    public async Task<PagedResult<MyReviewResponse>> GetMyReviewsAsync(Guid reviewerId, int page, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        const int pageSize = 10;
        var (items, total) = await _reviewRepo.GetByReviewerAsync(reviewerId, page, pageSize, cancellationToken);

        return new PagedResult<MyReviewResponse>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}

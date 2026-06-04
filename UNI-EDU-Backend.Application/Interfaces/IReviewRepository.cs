using UNI_EDU_Backend.Application.DTOs.Reviews;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

// Context needed to authorize + place a class-level tutor review.
public record ClassReviewContext(Guid TutorId, Guid StudentId, ClassStatus Status);

public interface IReviewRepository
{
    Task<ClassReviewContext?> GetClassForReviewAsync(Guid classId, CancellationToken cancellationToken);
    Task<bool> IsParentOfStudentAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken);
    Task<bool> HasReviewedAsync(Guid classId, Guid reviewerId, CancellationToken cancellationToken);

    // Inserts the review and recomputes the tutor's average rating (one transaction).
    Task<ReviewResponse> CreateAsync(Guid classId, Guid tutorId, Guid reviewerId, int rating, string? comment, CancellationToken cancellationToken);

    // Reviews written by the given reviewer (student/parent), newest first.
    Task<(List<MyReviewResponse> Items, int Total)> GetByReviewerAsync(Guid reviewerId, int page, int pageSize, CancellationToken cancellationToken);

    // Moderation listing. hidden=null → all, true → hidden only, false → visible only.
    Task<(List<ModerationReviewResponse> Items, int Total)> GetForModerationAsync(bool? hidden, int page, int pageSize, CancellationToken cancellationToken);

    // Hide/unhide a review; recomputes the tutor's average over visible reviews. False if not found.
    Task<bool> SetHiddenAsync(int reviewId, bool hidden, CancellationToken cancellationToken);
}

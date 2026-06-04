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
}

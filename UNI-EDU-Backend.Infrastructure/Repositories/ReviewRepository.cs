using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Reviews;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class ReviewRepository(ApplicationDbContext dbContext) : IReviewRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public Task<ClassReviewContext?> GetClassForReviewAsync(Guid classId, CancellationToken cancellationToken) =>
        _dbContext.Classes
            .AsNoTracking()
            .Where(c => c.ClassID == classId)
            .Select(c => new ClassReviewContext(c.TutorID, c.StudentID, c.Status))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> IsParentOfStudentAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken) =>
        _dbContext.Students.AnyAsync(s => s.StudentID == studentId && s.ParentID == parentId, cancellationToken);

    public Task<bool> HasReviewedAsync(Guid classId, Guid reviewerId, CancellationToken cancellationToken) =>
        _dbContext.Reviews.AnyAsync(r => r.ClassID == classId && r.ReviewerID == reviewerId, cancellationToken);

    public async Task<ReviewResponse> CreateAsync(Guid classId, Guid tutorId, Guid reviewerId, int rating, string? comment, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var review = new Review
        {
            ReviewerID = reviewerId,
            TutorID = tutorId,
            ClassID = classId,
            Rating = rating,
            Comment = comment ?? string.Empty,
            ReviewDate = now
        };
        _dbContext.Reviews.Add(review);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Recompute the tutor's average over all their reviews (now including this one).
        var average = await _dbContext.Reviews
            .Where(r => r.TutorID == tutorId)
            .AverageAsync(r => (double?)r.Rating, cancellationToken) ?? 0d;

        var tutor = await _dbContext.Tutors.FirstOrDefaultAsync(t => t.TutorID == tutorId, cancellationToken);
        if (tutor is not null)
            tutor.AverageRating = (float)average;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new ReviewResponse
        {
            Id = review.ReviewID,
            ClassId = classId,
            TutorId = tutorId,
            ReviewerId = reviewerId,
            Rating = rating,
            Comment = review.Comment,
            Date = now
        };
    }

    public async Task<(List<MyReviewResponse> Items, int Total)> GetByReviewerAsync(Guid reviewerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var q = _dbContext.Reviews.AsNoTracking().Where(r => r.ReviewerID == reviewerId);
        var total = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(r => r.ReviewDate)
            .ThenByDescending(r => r.ReviewID)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new MyReviewResponse
            {
                Id = r.ReviewID,
                ClassId = r.ClassID,
                ClassName = r.Class.Name,
                TutorId = r.TutorID,
                TutorName = r.Tutor.FullName ?? r.Tutor.User.Fullname,
                Subject = r.Class.Subject.SubjectName,
                Rating = r.Rating,
                Comment = r.Comment,
                Date = r.ReviewDate
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}

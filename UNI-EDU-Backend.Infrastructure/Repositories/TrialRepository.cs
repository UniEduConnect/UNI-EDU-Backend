using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Trials;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class TrialRepository(ApplicationDbContext dbContext) : ITrialRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<TrialResponse> CreateAsync(TrialBooking trial, CancellationToken cancellationToken)
    {
        _dbContext.TrialBookings.Add(trial);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectByIdAsync(trial.TrialID, cancellationToken);
    }

    public async Task<(List<TrialResponse> Items, int Total)> GetMyTrialsAsync(
        Guid callerUserId, string callerRole, TrialStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _dbContext.TrialBookings.AsNoTracking().AsQueryable();

        // Role scoping. The default branch yields an empty result rather than throwing —
        // authorization (which roles can list at all) is enforced in the service.
        query = callerRole switch
        {
            "Tutor" => query.Where(t => t.TutorID == callerUserId),
            "Student" => query.Where(t => t.StudentID == callerUserId),
            "Parent" => query.Where(t => t.Student.ParentID == callerUserId),
            "Admin" => query,
            _ => query.Where(_ => false)
        };

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenBy(t => t.TrialID)
            .Skip(skip)
            .Take(pageSize)
            .Select(t => new TrialResponse
            {
                Id = t.TrialID,
                TutorId = t.TutorID,
                TutorName = t.Tutor.FullName ?? string.Empty,
                StudentId = t.StudentID,
                StudentName = t.Student.FullName ?? string.Empty,
                ParentId = t.ParentID,
                SubjectId = t.SubjectID,
                SubjectName = t.Subject.SubjectName ?? string.Empty,
                RequestedAt = t.RequestedAt,
                Goals = t.Goals,
                CurrentLevel = t.CurrentLevel,
                Note = t.Note,
                Status = t.Status.ToString(),
                ReviewedAt = t.ReviewedAt,
                ReviewNote = t.ReviewNote,
                CompletedAt = t.CompletedAt,
                Feedback = t.Feedback,
                Rating = t.Rating,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<TrialBooking?> GetByIdAsync(Guid trialId, CancellationToken cancellationToken) =>
        _dbContext.TrialBookings.FirstOrDefaultAsync(t => t.TrialID == trialId, cancellationToken);

    public async Task<TrialResponse> ApplyTransitionAsync(TrialBooking trial, TrialStatus newStatus, string? reviewNote, CancellationToken cancellationToken)
    {
        trial.Status = newStatus;
        trial.ReviewedAt = DateTime.UtcNow;
        if (reviewNote is not null)
            trial.ReviewNote = reviewNote;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectByIdAsync(trial.TrialID, cancellationToken);
    }

    public async Task<TrialResponse> ApplyCompletionAsync(TrialBooking trial, string? feedback, double? rating, CancellationToken cancellationToken)
    {
        trial.Status = TrialStatus.Completed;
        trial.CompletedAt = DateTime.UtcNow;
        if (feedback is not null)
            trial.Feedback = feedback;
        if (rating.HasValue)
            trial.Rating = rating.Value;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectByIdAsync(trial.TrialID, cancellationToken);
    }

    private Task<TrialResponse> ProjectByIdAsync(Guid trialId, CancellationToken cancellationToken) =>
        _dbContext.TrialBookings
            .AsNoTracking()
            .Where(t => t.TrialID == trialId)
            .Select(t => new TrialResponse
            {
                Id = t.TrialID,
                TutorId = t.TutorID,
                TutorName = t.Tutor.FullName ?? string.Empty,
                StudentId = t.StudentID,
                StudentName = t.Student.FullName ?? string.Empty,
                ParentId = t.ParentID,
                SubjectId = t.SubjectID,
                SubjectName = t.Subject.SubjectName ?? string.Empty,
                RequestedAt = t.RequestedAt,
                Goals = t.Goals,
                CurrentLevel = t.CurrentLevel,
                Note = t.Note,
                Status = t.Status.ToString(),
                ReviewedAt = t.ReviewedAt,
                ReviewNote = t.ReviewNote,
                CompletedAt = t.CompletedAt,
                Feedback = t.Feedback,
                Rating = t.Rating,
                CreatedAt = t.CreatedAt
            })
            .FirstAsync(cancellationToken);
}

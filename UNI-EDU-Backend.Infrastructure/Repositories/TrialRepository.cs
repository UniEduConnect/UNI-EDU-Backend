using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Trials;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class TrialRepository(ApplicationDbContext dbContext) : ITrialRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public Task<bool> TutorExistsAsync(Guid tutorId, CancellationToken cancellationToken) =>
        _dbContext.Tutors.AnyAsync(t => t.TutorID == tutorId, cancellationToken);

    public Task<bool> SubjectExistsAsync(Guid subjectId, CancellationToken cancellationToken) =>
        _dbContext.Subjects.AnyAsync(s => s.SubjectID == subjectId, cancellationToken);

    public async Task<TrialResponse> CreateAsync(Guid studentId, Guid tutorId, CreateTrialRequest request, CancellationToken cancellationToken)
    {
        var entity = new TrialRequest
        {
            TrialRequestID = Guid.NewGuid(),
            StudentID = studentId,
            TutorID = tutorId,
            SubjectID = request.SubjectId,
            Day = request.Day.Trim(),
            Time = request.Time.Trim(),
            Message = request.Message,
            Status = TrialStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TrialRequests.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ByIdAsync(entity.TrialRequestID, cancellationToken);
    }

    public async Task<(List<TrialResponse> Items, int Total)> GetByStudentAsync(Guid studentId, TrialStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var q = _dbContext.TrialRequests.AsNoTracking().Where(t => t.StudentID == studentId);
        return await PageAsync(q, status, page, pageSize, cancellationToken);
    }

    public async Task<(List<TrialResponse> Items, int Total)> GetByTutorAsync(Guid tutorId, TrialStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var q = _dbContext.TrialRequests.AsNoTracking().Where(t => t.TutorID == tutorId);
        return await PageAsync(q, status, page, pageSize, cancellationToken);
    }

    public async Task<(TrialReviewOutcome Outcome, TrialResponse? Trial)> RespondAsync(Guid trialId, Guid tutorId, bool accept, CancellationToken cancellationToken)
    {
        var trial = await _dbContext.TrialRequests.FirstOrDefaultAsync(t => t.TrialRequestID == trialId, cancellationToken);
        if (trial is null) return (TrialReviewOutcome.NotFound, null);
        if (trial.TutorID != tutorId) return (TrialReviewOutcome.Forbidden, null);
        if (trial.Status != TrialStatus.Pending) return (TrialReviewOutcome.AlreadyResponded, null);

        trial.Status = accept ? TrialStatus.Accepted : TrialStatus.Declined;
        trial.RespondedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (TrialReviewOutcome.Done, await ByIdAsync(trialId, cancellationToken));
    }

    private async Task<(List<TrialResponse> Items, int Total)> PageAsync(IQueryable<TrialRequest> q, TrialStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (status is not null)
            q = q.Where(t => t.Status == status);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    private Task<TrialResponse> ByIdAsync(Guid trialId, CancellationToken cancellationToken) =>
        _dbContext.TrialRequests.AsNoTracking()
            .Where(t => t.TrialRequestID == trialId)
            .Select(Projection)
            .FirstAsync(cancellationToken);

    private static readonly Expression<Func<TrialRequest, TrialResponse>> Projection =
        t => new TrialResponse
        {
            Id = t.TrialRequestID,
            StudentId = t.StudentID,
            StudentName = t.Student.FullName ?? t.Student.User.Fullname,
            TutorId = t.TutorID,
            TutorName = t.Tutor.FullName ?? t.Tutor.User.Fullname,
            SubjectId = t.SubjectID,
            Subject = t.Subject != null ? t.Subject.SubjectName : null,
            Day = t.Day,
            Time = t.Time,
            Message = t.Message,
            Status = t.Status == TrialStatus.Accepted ? "accepted" : t.Status == TrialStatus.Declined ? "declined" : "pending",
            CreatedAt = t.CreatedAt,
            RespondedAt = t.RespondedAt
        };
}

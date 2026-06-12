using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.TutorPosts;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class TutorPostRepository(ApplicationDbContext dbContext) : ITutorPostRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task CreateAsync(Guid tutorId, CreateTutorPostRequest request, CancellationToken cancellationToken)
    {
        _dbContext.Set<TutorPost>().Add(new TutorPost
        {
            Id = Guid.NewGuid(),
            TutorId = tutorId,
            SubjectId = request.SubjectId,
            GradeLevels = request.GradeLevels,
            HourlyRate = request.HourlyRate,
            PreferredSchedule = request.PreferredSchedule,
            Note = request.Note,
            Status = "open",
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<TutorPostResponse>> GetOpenAsync(TutorPostListQuery query, int pageSize, CancellationToken cancellationToken)
    {
        var q = _dbContext.Set<TutorPost>().AsNoTracking().Where(p => p.Status == "open");

        if (!string.IsNullOrWhiteSpace(query.Subject))
            q = q.Where(p => p.Subject.SubjectName == query.Subject);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = $"%{query.Search.Trim()}%";
            q = q.Where(p => EF.Functions.ILike(p.Tutor.FullName, s) || EF.Functions.ILike(p.Subject.SubjectName, s));
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<TutorPostResponse> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<List<TutorPostResponse>> GetMineAsync(Guid tutorId, CancellationToken cancellationToken) =>
        await _dbContext.Set<TutorPost>().AsNoTracking()
            .Where(p => p.TutorId == tutorId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(MapProjection)
            .ToListAsync(cancellationToken);

    public async Task<bool> CloseAsync(Guid postId, Guid tutorId, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Set<TutorPost>()
            .FirstOrDefaultAsync(p => p.Id == postId && p.TutorId == tutorId, cancellationToken);
        if (post is null) return false;

        post.Status = "closed";
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(Guid TutorId, Guid SubjectId)?> GetOpenPostForApplyAsync(Guid postId, CancellationToken cancellationToken)
    {
        var row = await _dbContext.Set<TutorPost>().AsNoTracking()
            .Where(p => p.Id == postId && p.Status == "open")
            .Select(p => new { p.TutorId, p.SubjectId })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : (row.TutorId, row.SubjectId);
    }

    public Task<bool> HasPendingApplicationAsync(Guid postId, Guid studentId, CancellationToken cancellationToken) =>
        _dbContext.Set<TutorPostApplication>()
            .AnyAsync(a => a.TutorPostId == postId && a.StudentId == studentId && a.Status == "pending", cancellationToken);

    public async Task CreateApplicationAsync(Guid postId, Guid studentId, Guid tutorId, Guid subjectId, CancellationToken cancellationToken)
    {
        _dbContext.Set<TutorPostApplication>().Add(new TutorPostApplication
        {
            Id = Guid.NewGuid(),
            TutorPostId = postId,
            StudentId = studentId,
            TutorId = tutorId,
            SubjectId = subjectId,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GetStudentNameAsync(Guid studentId, CancellationToken cancellationToken) =>
        await _dbContext.Students.AsNoTracking()
            .Where(s => s.StudentID == studentId)
            .Select(s => s.FullName ?? s.User.Fullname)
            .FirstOrDefaultAsync(cancellationToken) ?? "Học sinh";

    public async Task<List<TutorPostApplicationResponse>> GetApplicationsForTutorAsync(Guid tutorId, CancellationToken cancellationToken) =>
        await _dbContext.Set<TutorPostApplication>().AsNoTracking()
            .Where(a => a.TutorId == tutorId && a.Status == "pending")
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new TutorPostApplicationResponse
            {
                Id = a.Id,
                TutorPostId = a.TutorPostId,
                StudentName = a.Student.FullName ?? a.Student.User.Fullname,
                SubjectId = a.SubjectId,
                Subject = a.TutorPost.Subject.SubjectName,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

    public async Task<(Guid SubjectId, Guid StudentId, string Status)?> GetApplicationForAcceptAsync(Guid appId, Guid tutorId, CancellationToken cancellationToken)
    {
        var row = await _dbContext.Set<TutorPostApplication>().AsNoTracking()
            .Where(a => a.Id == appId && a.TutorId == tutorId)
            .Select(a => new { a.SubjectId, a.StudentId, a.Status })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : (row.SubjectId, row.StudentId, row.Status);
    }

    public async Task AcceptApplicationAsync(Guid appId, CancellationToken cancellationToken)
    {
        var app = await _dbContext.Set<TutorPostApplication>().FirstOrDefaultAsync(a => a.Id == appId, cancellationToken);
        if (app is null) return;
        app.Status = "accepted";
        app.RespondedAt = DateTime.UtcNow;

        // Materialize a real Class so the match shows up in everyone's class list.
        // No escrow — fee/schedule are arranged later between tutor and student.
        var post = await _dbContext.Set<TutorPost>().AsNoTracking()
            .Where(p => p.Id == app.TutorPostId)
            .Select(p => new { p.HourlyRate, SubjectName = p.Subject.SubjectName })
            .FirstOrDefaultAsync(cancellationToken);

        _dbContext.Classes.Add(new Class
        {
            ClassID = Guid.NewGuid(),
            TutorID = app.TutorId,
            StudentID = app.StudentId,
            SubjectID = app.SubjectId,
            Name = $"Lớp {post?.SubjectName ?? "học"}",
            StartDate = DateTime.UtcNow,
            TuitionFee = post?.HourlyRate ?? 0,
            Status = ClassStatus.Active,
            Format = ClassFormat.Online,
            WeeklySlots = new(),
            TotalSessions = 0,
            CompletedSessions = 0,
            EscrowAmount = 0,
            EscrowReleased = 0,
            EscrowStatus = EscrowStatus.Pending,
            ReleaseMilestone = 0,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // Expression (not a method) so EF Core translates it to a SQL projection with
    // the proper JOINs — a method call would be client-evaluated against unloaded
    // navigations (Tutor/Subject/User), throwing NullReferenceException.
    private static readonly Expression<Func<TutorPost, TutorPostResponse>> MapProjection = p => new TutorPostResponse
    {
        Id = p.Id,
        TutorId = p.TutorId,
        TutorName = p.Tutor.FullName ?? p.Tutor.User.Fullname,
        TutorAvatar = p.Tutor.AvatarUrl,
        Rating = p.Tutor.AverageRating ?? 0,
        SubjectId = p.SubjectId,
        Subject = p.Subject.SubjectName,
        GradeLevels = p.GradeLevels,
        HourlyRate = p.HourlyRate,
        PreferredSchedule = p.PreferredSchedule,
        Note = p.Note,
        Status = p.Status,
        CreatedAt = p.CreatedAt
    };
}

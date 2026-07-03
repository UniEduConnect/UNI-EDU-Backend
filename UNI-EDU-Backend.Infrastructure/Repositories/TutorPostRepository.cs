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
            DurationMonths = request.DurationMonths,
            Note = request.Note,
            Status = "open",
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<TutorPostResponse>> GetOpenAsync(TutorPostListQuery query, int pageSize, Guid callerId, CancellationToken cancellationToken)
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
            // callerId is captured in the closure so EF translates the pending-application
            // check into a correlated EXISTS subquery per row.
            .Select(p => new TutorPostResponse
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
                DurationMonths = p.DurationMonths,
                Note = p.Note,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                HasPendingApplication = _dbContext.Set<TutorPostApplication>()
                    .Any(a => a.TutorPostId == p.Id && a.StudentId == callerId && a.Status == "pending")
            })
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

    public async Task<(Guid SubjectId, Guid StudentId, string Status, string PostStatus)?> GetApplicationForAcceptAsync(Guid appId, Guid tutorId, CancellationToken cancellationToken)
    {
        var row = await _dbContext.Set<TutorPostApplication>().AsNoTracking()
            .Where(a => a.Id == appId && a.TutorId == tutorId)
            .Select(a => new { a.SubjectId, a.StudentId, a.Status, PostStatus = a.TutorPost.Status })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : (row.SubjectId, row.StudentId, row.Status, row.PostStatus);
    }

    public async Task AcceptApplicationAsync(Guid appId, CancellationToken cancellationToken)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var app = await _dbContext.Set<TutorPostApplication>().FirstOrDefaultAsync(a => a.Id == appId, cancellationToken);
        if (app is null) return;
        app.Status = "accepted";
        app.RespondedAt = DateTime.UtcNow;

        // Materialize a real Class so the match shows up in everyone's class list.
        // No escrow — fee is arranged later between tutor and student.
        var post = await _dbContext.Set<TutorPost>().AsNoTracking()
            .Where(p => p.Id == app.TutorPostId)
            .Select(p => new { p.HourlyRate, p.PreferredSchedule, p.DurationMonths, SubjectName = p.Subject.SubjectName })
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var classId = Guid.NewGuid();

        // Same timetable treatment as ClassRequestRepository.AssignAsync: parse the tutor's
        // posted schedule, start next week, and pre-generate Session rows sized to the
        // tutor's posted DurationMonths commitment (falls back to 3 months if unset — old
        // posts created before this field existed).
        var weeklySlots = SessionScheduling.ParsePreferredSchedule(post?.PreferredSchedule);
        var startDate = SessionScheduling.NextWeekMonday(now);
        var totalSessions = weeklySlots.Count > 0
            ? weeklySlots.Count * SessionScheduling.WeeksForDuration(post?.DurationMonths)
            : 0;

        var classRow = new Class
        {
            ClassID = classId,
            TutorID = app.TutorId,
            StudentID = app.StudentId,
            SubjectID = app.SubjectId,
            Name = $"Lớp {post?.SubjectName ?? "học"}",
            StartDate = startDate,
            TuitionFee = post?.HourlyRate ?? 0,
            Status = ClassStatus.Active,
            Format = ClassFormat.Online,
            WeeklySlots = weeklySlots,
            TotalSessions = totalSessions,
            CompletedSessions = 0,
            EscrowAmount = 0,
            EscrowReleased = 0,
            EscrowStatus = EscrowStatus.Pending,
            ReleaseMilestone = 0,
            CreatedAt = now
        };

        var sessions = SessionScheduling.BuildPlaceholderSessions(
            classId, classRow.Format, startDate, weeklySlots, totalSessions, now);

        _dbContext.Classes.Add(classRow);
        _dbContext.Sessions.AddRange(sessions);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Close the post — it's a single "slot"; once matched into a class it drops off
        // both the tutor's own post list ("Đã đóng") and every student's open-posts browse list.
        await _dbContext.Set<TutorPost>()
            .Where(p => p.Id == app.TutorPostId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "closed"), cancellationToken);

        await tx.CommitAsync(cancellationToken);
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
        DurationMonths = p.DurationMonths,
        Note = p.Note,
        Status = p.Status,
        CreatedAt = p.CreatedAt
    };
}

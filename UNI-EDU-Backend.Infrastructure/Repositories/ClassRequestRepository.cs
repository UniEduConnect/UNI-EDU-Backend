using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.ClassRequests;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;
using UNI_EDU_Backend.Infrastructure.Common;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class ClassRequestRepository(ApplicationDbContext dbContext) : IClassRequestRepository
{
    private const double PassThreshold = 70.0;
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task CreateAsync(Guid studentId, CreateClassRequestRequest request, CancellationToken cancellationToken)
    {
        // Validate at submit time: the student's requested schedule must not clash with itself or
        // with a class they're already enrolled in — so a conflict surfaces now, not only if/when a
        // tutor later accepts.
        await ScheduleConflict.EnsureRequestedScheduleFreeAsync(
            _dbContext, studentId, isTutor: false, request.PreferredSchedule, request.DurationMonths, cancellationToken);

        _dbContext.Set<ClassRequest>().Add(new ClassRequest
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            SubjectId = request.SubjectId,
            Grade = request.Grade,
            PreferredSchedule = request.PreferredSchedule,
            Budget = request.Budget,
            DurationMonths = request.DurationMonths,
            Note = request.Note,
            Status = "open",
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<ClassRequestResponse>> GetOpenAsync(ClassRequestListQuery query, int pageSize, CancellationToken cancellationToken)
    {
        var q = _dbContext.Set<ClassRequest>().AsNoTracking().Where(r => r.Status == "open");

        if (!string.IsNullOrWhiteSpace(query.Subject))
            q = q.Where(r => r.Subject.SubjectName == query.Subject);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = $"%{query.Search.Trim()}%";
            q = q.Where(r => EF.Functions.ILike(r.Student.FullName, s) || EF.Functions.ILike(r.Subject.SubjectName, s));
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<ClassRequestResponse> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<List<ClassRequestResponse>> GetMineAsync(Guid studentId, CancellationToken cancellationToken) =>
        await _dbContext.Set<ClassRequest>().AsNoTracking()
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(MapProjection)
            .ToListAsync(cancellationToken);

    public async Task<(string Status, Guid StudentId, Guid SubjectId)?> GetAcceptInfoAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var row = await _dbContext.Set<ClassRequest>().AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(r => new { r.Status, r.StudentId, r.SubjectId })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : (row.Status, row.StudentId, row.SubjectId);
    }

    public async Task<Guid> AssignAsync(Guid requestId, Guid tutorId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Atomically claim the request: the WHERE Status == "open" + row lock means only ONE of two
        // concurrent accepts (double-click / retry) flips it and proceeds to charge the wallet; the
        // loser updates 0 rows and bails before any money moves.
        var claimed = await _dbContext.Set<ClassRequest>()
            .Where(r => r.Id == requestId && r.Status == "open")
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, "assigned")
                .SetProperty(r => r.AssignedTutorId, tutorId)
                .SetProperty(r => r.AssignedAt, now), cancellationToken);
        if (claimed == 0) return Guid.Empty;

        var req = await _dbContext.Set<ClassRequest>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (req is null) return Guid.Empty;

        // Materialize a real Class so the match shows up in everyone's class list
        // (tutor / student / parent).
        var subjectName = await _dbContext.Subjects.AsNoTracking()
            .Where(s => s.SubjectID == req.SubjectId)
            .Select(s => s.SubjectName)
            .FirstOrDefaultAsync(cancellationToken) ?? "Lớp học";

        var classId = Guid.NewGuid();

        // Build the timetable from the student's requested schedule (e.g. "T2 19:00-21:00,
        // T5 18:00-20:00"): the class starts on the Monday of NEXT week, and we pre-generate
        // Session rows to cover the requested minimum commitment (DurationMonths).
        var weeklySlots = SessionScheduling.ParsePreferredSchedule(req.PreferredSchedule);
        var startDate = SessionScheduling.NextWeekMonday(now);
        var totalSessions = weeklySlots.Count > 0
            ? weeklySlots.Count * SessionScheduling.WeeksForDuration(req.DurationMonths)
            : 0;

        // Full tuition = per-session budget × number of sessions — charged from the student's
        // wallet into escrow now (see ChargeStudentAsync below).
        var perSessionFee = req.Budget ?? 0;
        var totalTuition = ClassEscrow.TotalTuition(perSessionFee, totalSessions);

        var classRow = new Class
        {
            ClassID = classId,
            TutorID = tutorId,
            StudentID = req.StudentId,
            SubjectID = req.SubjectId,
            Name = $"Lớp {subjectName}",
            StartDate = startDate,
            // TuitionFee mirrors EscrowAmount (the class total) — the codebase-wide convention the
            // direct-booking flow and the admin fee-edit reconciliation both rely on.
            TuitionFee = totalTuition,
            Status = ClassStatus.Active,
            Format = ClassFormat.Online,
            WeeklySlots = weeklySlots,
            TotalSessions = totalSessions,
            CompletedSessions = 0,
            EscrowAmount = totalTuition,
            EscrowReleased = 0,
            EscrowStatus = EscrowStatus.Pending,
            ReleaseMilestone = 0,
            CreatedAt = now
        };

        var sessions = SessionScheduling.BuildPlaceholderSessions(
            classId, classRow.Format, startDate, weeklySlots, totalSessions, now);

        // Block double-booking, then charge the student — both before any write, so an
        // underfunded wallet or a clashing slot aborts the whole accept (one SaveChanges = atomic).
        await ScheduleConflict.EnsureNoConflictAsync(_dbContext, tutorId, req.StudentId, sessions, cancellationToken);
        await ClassEscrow.ChargeStudentAsync(_dbContext, req.StudentId, classId, classRow.Name, totalTuition, now, cancellationToken);

        _dbContext.Classes.Add(classRow);
        _dbContext.Sessions.AddRange(sessions);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return classId;
    }

    // Read-only pre-check: reproduces AssignAsync's would-be timetable + tuition EXACTLY, then runs
    // the same conflict + affordability guards WITHOUT any write, status change, or test consumption.
    // Keep the session/tuition build byte-for-byte identical to AssignAsync above.
    public async Task EnsureAcceptableAsync(Guid tutorId, Guid requestId, CancellationToken cancellationToken)
    {
        var req = await _dbContext.Set<ClassRequest>().AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(r => new { r.Status, r.StudentId, r.SubjectId, r.PreferredSchedule, r.Budget, r.DurationMonths })
            .FirstOrDefaultAsync(cancellationToken);

        if (req is null)
            throw new NotFoundException("Không tìm thấy yêu cầu tìm gia sư.");
        if (req.Status != "open")
            throw new BadRequestException("Yêu cầu này đã được nhận hoặc đã đóng.");

        var now = DateTime.UtcNow;

        var weeklySlots = SessionScheduling.ParsePreferredSchedule(req.PreferredSchedule);
        var startDate = SessionScheduling.NextWeekMonday(now);
        var totalSessions = weeklySlots.Count > 0
            ? weeklySlots.Count * SessionScheduling.WeeksForDuration(req.DurationMonths)
            : 0;

        var perSessionFee = req.Budget ?? 0;
        var totalTuition = ClassEscrow.TotalTuition(perSessionFee, totalSessions);

        var sessions = SessionScheduling.BuildPlaceholderSessions(
            Guid.NewGuid(), ClassFormat.Online, startDate, weeklySlots, totalSessions, now);

        // Both-party clash guard, then read-only affordability guard — no writes at all.
        await ScheduleConflict.EnsureNoConflictAsync(_dbContext, tutorId, req.StudentId, sessions, cancellationToken);
        await ClassEscrow.EnsureAffordableAsync(_dbContext, req.StudentId, totalTuition, cancellationToken);
    }

    // Expression (not a method) so EF Core translates it to a SQL projection with
    // JOINs — a method call would be client-evaluated against unloaded navigations
    // (Student/Subject/User), throwing NullReferenceException.
    private static readonly Expression<Func<ClassRequest, ClassRequestResponse>> MapProjection = r => new ClassRequestResponse
    {
        Id = r.Id,
        StudentId = r.StudentId,
        StudentName = r.Student.FullName ?? r.Student.User.Fullname,
        School = r.Student.School ?? string.Empty,
        Grade = r.Grade,
        SubjectId = r.SubjectId,
        Subject = r.Subject.SubjectName,
        PreferredSchedule = r.PreferredSchedule,
        Budget = r.Budget,
        DurationMonths = r.DurationMonths,
        Note = r.Note,
        Status = r.Status,
        AssignedTutorName = null,
        CreatedAt = r.CreatedAt
    };
}

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.ClassRequests;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class ClassRequestRepository(ApplicationDbContext dbContext) : IClassRequestRepository
{
    private const double PassThreshold = 70.0;
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task CreateAsync(Guid studentId, CreateClassRequestRequest request, CancellationToken cancellationToken)
    {
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

    public async Task AssignAsync(Guid requestId, Guid tutorId, CancellationToken cancellationToken)
    {
        var req = await _dbContext.Set<ClassRequest>().FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (req is null) return;

        req.Status = "assigned";
        req.AssignedTutorId = tutorId;
        req.AssignedAt = DateTime.UtcNow;

        // Materialize a real Class so the match shows up in everyone's class list
        // (tutor / student / parent). No escrow — fee/schedule are arranged later.
        var subjectName = await _dbContext.Subjects.AsNoTracking()
            .Where(s => s.SubjectID == req.SubjectId)
            .Select(s => s.SubjectName)
            .FirstOrDefaultAsync(cancellationToken) ?? "Lớp học";

        _dbContext.Classes.Add(new Class
        {
            ClassID = Guid.NewGuid(),
            TutorID = tutorId,
            StudentID = req.StudentId,
            SubjectID = req.SubjectId,
            Name = $"Lớp {subjectName}",
            StartDate = DateTime.UtcNow,
            TuitionFee = req.Budget ?? 0,
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

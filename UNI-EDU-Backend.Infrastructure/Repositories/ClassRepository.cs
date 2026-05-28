using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class ClassRepository(ApplicationDbContext dbContext) : IClassRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public Task<bool> TutorExistsAsync(Guid tutorId, CancellationToken cancellationToken) =>
        _dbContext.Tutors.AnyAsync(t => t.TutorID == tutorId, cancellationToken);

    public Task<bool> StudentExistsAsync(Guid studentId, CancellationToken cancellationToken) =>
        _dbContext.Students.AnyAsync(s => s.StudentID == studentId, cancellationToken);

    public Task<bool> IsParentOfStudentAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken) =>
        _dbContext.Students.AnyAsync(s => s.StudentID == studentId && s.ParentID == parentId, cancellationToken);

    public Task<string?> GetSubjectNameAsync(Guid subjectId, CancellationToken cancellationToken) =>
        _dbContext.Subjects
            .Where(s => s.SubjectID == subjectId)
            .Select(s => s.SubjectName)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ClassResponse> CreateClassWithEscrowAsync(CreateClassRequest request, CancellationToken cancellationToken)
    {
        var format = ParseFormat(request.Format);
        var now = DateTime.UtcNow;

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var wallet = await _dbContext.Wallets
            .FirstOrDefaultAsync(w => w.UserID == request.StudentId, cancellationToken)
            ?? throw new BadRequestException("Student wallet not found. Top up before booking.");

        if (wallet.Balance < request.Fee)
            throw new BadRequestException($"Insufficient wallet balance. Required: {request.Fee}, available: {wallet.Balance}.");

        var subjectName = await GetSubjectNameAsync(request.SubjectId, cancellationToken)
            ?? throw new NotFoundException($"Subject with id '{request.SubjectId}' not found.");

        var weeklySlots = request.WeeklySlots
            .Select(s => new ClassScheduleSlot
            {
                DayOfWeek = (DayOfWeek)s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            })
            .ToList();

        var classRow = new Class
        {
            ClassID = Guid.NewGuid(),
            TutorID = request.TutorId,
            StudentID = request.StudentId,
            SubjectID = request.SubjectId,
            Name = string.IsNullOrWhiteSpace(request.Name) ? $"{subjectName} - Lớp mới" : request.Name,
            StartDate = request.StartDate,
            TuitionFee = request.Fee,
            Status = ClassStatus.Searching,
            Format = format,
            WeeklySlots = weeklySlots,
            TotalSessions = request.TotalSessions,
            CompletedSessions = 0,
            EscrowAmount = request.Fee,
            EscrowReleased = 0m,
            EscrowStatus = EscrowStatus.Pending,
            ReleaseMilestone = 1,
            CreatedAt = now
        };

        wallet.Balance -= request.Fee;
        wallet.EscrowBalance += request.Fee;
        wallet.UpdatedAt = now;

        var escrowTx = new WalletTransaction
        {
            TransactionID = Guid.NewGuid(),
            UserID = request.StudentId,
            Type = WalletTxType.EscrowIn,
            Amount = request.Fee,
            RelatedClassID = classRow.ClassID,
            Description = $"Escrow funding for class {classRow.Name}",
            CreatedAt = now
        };

        var sessions = BuildPlaceholderSessions(classRow, request.TotalSessions, now);

        _dbContext.Classes.Add(classRow);
        _dbContext.WalletTransactions.Add(escrowTx);
        _dbContext.Sessions.AddRange(sessions);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new ClassResponse
        {
            Id = classRow.ClassID,
            Name = classRow.Name,
            TutorId = classRow.TutorID,
            StudentId = classRow.StudentID,
            SubjectId = classRow.SubjectID,
            Subject = subjectName,
            Format = format.ToString().ToLowerInvariant(),
            Fee = classRow.TuitionFee,
            Status = classRow.Status.ToString().ToLowerInvariant(),
            WeeklySlots = classRow.WeeklySlots
                .Select(s => new WeeklySlotDto
                {
                    DayOfWeek = (int)s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                })
                .ToList(),
            TotalSessions = classRow.TotalSessions,
            CompletedSessions = classRow.CompletedSessions,
            EscrowAmount = classRow.EscrowAmount,
            EscrowReleased = classRow.EscrowReleased,
            EscrowStatus = classRow.EscrowStatus.ToString().ToLowerInvariant(),
            CreatedAt = classRow.CreatedAt,
            StartDate = classRow.StartDate,
            Sessions = sessions
                .OrderBy(s => s.StartAt)
                .Select(s => new SessionResponse
                {
                    Id = s.SessionID,
                    ClassId = s.ClassID,
                    StartAt = s.StartAt,
                    EndAt = s.EndAt,
                    Status = s.Status.ToString().ToLowerInvariant(),
                    Format = s.Format.ToString().ToLowerInvariant()
                })
                .ToList()
        };
    }

    /// <summary>
    /// Walk the calendar forward from <c>StartDate</c>. For every day that matches a configured
    /// <c>WeeklySlots</c> entry, emit one session per slot (ordered by start time) until
    /// <paramref name="total"/> sessions are placed.
    /// </summary>
    private static List<Session> BuildPlaceholderSessions(Class c, int total, DateTime now)
    {
        var sessions = new List<Session>(total);
        if (total <= 0 || c.WeeklySlots == null || c.WeeklySlots.Count == 0)
            return sessions;

        var slotsByDay = c.WeeklySlots
            .GroupBy(s => s.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.StartTime).ToList());

        var cursor = DateOnly.FromDateTime(c.StartDate);
        // Worst case: 1 session/week -> total weeks. Cap walk at total*7 + 7 days as a safety net.
        var maxDaysToWalk = (total * 7) + 7;

        for (var dayOffset = 0; dayOffset < maxDaysToWalk && sessions.Count < total; dayOffset++)
        {
            var date = cursor.AddDays(dayOffset);
            if (!slotsByDay.TryGetValue(date.DayOfWeek, out var daySlots)) continue;

            foreach (var slot in daySlots)
            {
                if (sessions.Count >= total) break;

                sessions.Add(new Session
                {
                    SessionID = Guid.NewGuid(),
                    ClassID = c.ClassID,
                    StartAt = date.ToDateTime(slot.StartTime, DateTimeKind.Utc),
                    EndAt = date.ToDateTime(slot.EndTime, DateTimeKind.Utc),
                    Status = SessionStatus.Scheduled,
                    Format = c.Format,
                    CreatedAt = now
                });
            }
        }

        return sessions;
    }

    private static ClassFormat ParseFormat(string raw) =>
        (raw ?? string.Empty).ToLowerInvariant() switch
        {
            "offline" => ClassFormat.Offline,
            "hybrid" => ClassFormat.Hybrid,
            _ => ClassFormat.Online
        };

    public async Task<(List<ClassListItemResponse> Items, int Total)> GetMyClassesAsync(
        Guid callerUserId, string callerRole, ClassStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _dbContext.Classes.AsNoTracking().AsQueryable();

        // Role scoping. Admin sees all; unknown roles already rejected by the service.
        query = callerRole switch
        {
            "Tutor" => query.Where(c => c.TutorID == callerUserId),
            "Student" => query.Where(c => c.StudentID == callerUserId),
            "Parent" => query.Where(c => c.Student.ParentID == callerUserId),
            _ => query
        };

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        // Pull WeeklySlots raw (jsonb-converted) — map to DTOs client-side after materialization.
        var raw = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenBy(c => c.ClassID)
            .Skip(skip)
            .Take(pageSize)
            .Select(c => new
            {
                c.ClassID,
                c.Name,
                c.TutorID,
                c.StudentID,
                c.SubjectID,
                SubjectName = c.Subject.SubjectName,
                TutorName = c.Tutor.FullName,
                TutorAvatar = c.Tutor.AvatarUrl,
                StudentName = c.Student.FullName,
                c.Status,
                c.Format,
                c.TuitionFee,
                c.TotalSessions,
                c.CompletedSessions,
                c.StartDate,
                c.CreatedAt,
                c.WeeklySlots
            })
            .ToListAsync(cancellationToken);

        var items = raw.Select(c => new ClassListItemResponse
        {
            Id = c.ClassID,
            Name = c.Name,
            TutorId = c.TutorID,
            StudentId = c.StudentID,
            SubjectId = c.SubjectID,
            Subject = c.SubjectName,
            TutorName = c.TutorName,
            TutorAvatar = c.TutorAvatar ?? string.Empty,
            StudentName = c.StudentName,
            Status = c.Status.ToString().ToLowerInvariant(),
            Format = c.Format.ToString().ToLowerInvariant(),
            Fee = c.TuitionFee,
            TotalSessions = c.TotalSessions,
            CompletedSessions = c.CompletedSessions,
            StartDate = c.StartDate,
            CreatedAt = c.CreatedAt,
            WeeklySlots = (c.WeeklySlots ?? new List<ClassScheduleSlot>())
                .Select(s => new WeeklySlotDto
                {
                    DayOfWeek = (int)s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                })
                .ToList()
        }).ToList();

        return (items, total);
    }

    public async Task<bool> UpdatePartialAsync(Guid classId, string? name, ClassStatus? status, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Classes
            .FirstOrDefaultAsync(c => c.ClassID == classId, cancellationToken);

        if (entity is null) return false;

        if (name is not null) entity.Name = name.Trim();
        if (status.HasValue) entity.Status = status.Value;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ClassDetailResponse?> GetByIdAsync(Guid classId, CancellationToken cancellationToken)
    {
        // Single roundtrip for the class + denormalized display fields.
        var raw = await _dbContext.Classes
            .AsNoTracking()
            .Where(c => c.ClassID == classId)
            .Select(c => new
            {
                c.ClassID,
                c.Name,
                c.TutorID,
                c.StudentID,
                c.SubjectID,
                c.StartDate,
                c.TuitionFee,
                c.Status,
                c.Format,
                c.WeeklySlots,
                c.TotalSessions,
                c.CompletedSessions,
                c.EscrowAmount,
                c.EscrowReleased,
                c.EscrowStatus,
                c.CreatedAt,
                c.Subject.SubjectName,
                TutorName = c.Tutor.FullName,
                TutorAvatar = c.Tutor.AvatarUrl,
                c.Tutor.TutorType,
                StudentName = c.Student.FullName,
                ParentName = c.Student.Parent != null ? c.Student.Parent.FullName : string.Empty
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (raw == null) return null;

        // Sessions ordered chronologically.
        var sessions = await _dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.ClassID == classId)
            .OrderBy(s => s.StartAt)
            .Select(s => new SessionResponse
            {
                Id = s.SessionID,
                ClassId = s.ClassID,
                StartAt = s.StartAt,
                EndAt = s.EndAt,
                Status = s.Status.ToString().ToLowerInvariant(),
                Format = s.Format.ToString().ToLowerInvariant()
            })
            .ToListAsync(cancellationToken);

        // Materials ordered newest-first (matches how the UI usually shows uploads).
        var materials = await _dbContext.ClassMaterials
            .AsNoTracking()
            .Where(m => m.ClassID == classId)
            .OrderByDescending(m => m.UploadedAt)
            .Select(m => new MaterialResponse
            {
                Id = m.MaterialID,
                ClassId = m.ClassID,
                Name = m.Name,
                Type = m.Type,
                Url = m.Url,
                Size = m.Size,
                UploadedAt = m.UploadedAt
            })
            .ToListAsync(cancellationToken);

        return new ClassDetailResponse
        {
            Id = raw.ClassID,
            Name = raw.Name,
            TutorId = raw.TutorID,
            StudentId = raw.StudentID,
            SubjectId = raw.SubjectID,
            Subject = raw.SubjectName,
            TutorName = raw.TutorName,
            TutorAvatar = raw.TutorAvatar ?? string.Empty,
            TutorType = raw.TutorType == TutorType.Teacher ? "teacher" : "tutor",
            StudentName = raw.StudentName,
            StudentAvatar = string.Empty, // Student entity has no avatar field today
            ParentName = raw.ParentName,
            Format = raw.Format.ToString().ToLowerInvariant(),
            Fee = raw.TuitionFee,
            Status = raw.Status.ToString().ToLowerInvariant(),
            WeeklySlots = (raw.WeeklySlots ?? new List<ClassScheduleSlot>())
                .Select(s => new WeeklySlotDto
                {
                    DayOfWeek = (int)s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                })
                .ToList(),
            TotalSessions = raw.TotalSessions,
            CompletedSessions = raw.CompletedSessions,
            EscrowAmount = raw.EscrowAmount,
            EscrowReleased = raw.EscrowReleased,
            EscrowStatus = raw.EscrowStatus.ToString().ToLowerInvariant(),
            CreatedAt = raw.CreatedAt,
            StartDate = raw.StartDate,
            Sessions = sessions,
            Materials = materials
        };
    }
}

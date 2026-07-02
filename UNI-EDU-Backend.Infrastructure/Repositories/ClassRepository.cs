using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.Commons;
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
            // StartDate comes from a JSON date ("yyyy-MM-dd") with Kind=Unspecified, which
            // Npgsql rejects for a `timestamp with time zone` column. Normalize to UTC.
            StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc),
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

    // Walks the calendar forward from StartDate against WeeklySlots to emit placeholder
    // Session rows — delegates to the shared SessionScheduling helper (also used by
    // ClassRequestRepository when a class is created from an accepted class request).
    private static List<Session> BuildPlaceholderSessions(Class c, int total, DateTime now) =>
        SessionScheduling.BuildPlaceholderSessions(c.ClassID, c.Format, c.StartDate, c.WeeklySlots ?? new(), total, now);

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

    public async Task<bool> UpdatePartialAsync(Guid classId, UpdateClassRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Classes
            .FirstOrDefaultAsync(c => c.ClassID == classId, cancellationToken);

        if (entity is null) return false;

        if (request.Name is not null) entity.Name = request.Name.Trim();
        if (request.Status is not null && Enum.TryParse<ClassStatus>(request.Status.Trim(), true, out var st))
            entity.Status = st;
        if (request.SubjectId is not null) entity.SubjectID = request.SubjectId.Value;
        if (request.TutorId is not null) entity.TutorID = request.TutorId.Value;
        if (request.StudentId is not null) entity.StudentID = request.StudentId.Value;
        if (request.Format is not null) entity.Format = ParseFormat(request.Format);
        if (request.TotalSessions is not null) entity.TotalSessions = request.TotalSessions.Value;

        // Fee change → re-sync the student's escrow so EscrowAmount stays equal to the fee.
        // Increase: lock the extra from the student's Balance. Decrease: refund the surplus.
        if (request.Fee is not null && request.Fee.Value != entity.EscrowAmount)
        {
            var newFee = request.Fee.Value;
            if (newFee < entity.EscrowReleased)
                throw new BadRequestException($"Học phí mới ({newFee:N0}) không thể nhỏ hơn phần ký quỹ đã giải ngân ({entity.EscrowReleased:N0}).");

            var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserID == entity.StudentID, cancellationToken)
                ?? throw new BadRequestException("Không tìm thấy ví của học sinh để điều chỉnh ký quỹ.");

            var delta = newFee - entity.EscrowAmount; // >0 lock more, <0 release back
            var now = DateTime.UtcNow;

            if (delta > 0)
            {
                if (wallet.Balance < delta)
                    throw new BadRequestException($"Số dư ví học sinh không đủ để tăng ký quỹ thêm {delta:N0}đ. Hãy nạp thêm trước.");
                wallet.Balance -= delta;
                wallet.EscrowBalance += delta;
                _dbContext.WalletTransactions.Add(new WalletTransaction
                {
                    TransactionID = Guid.NewGuid(), UserID = entity.StudentID, Type = WalletTxType.EscrowIn,
                    Amount = delta, RelatedClassID = entity.ClassID,
                    Description = $"Điều chỉnh tăng ký quỹ lớp {entity.Name}", CreatedAt = now
                });
            }
            else
            {
                var refund = -delta;
                wallet.EscrowBalance -= refund;
                wallet.Balance += refund;
                _dbContext.WalletTransactions.Add(new WalletTransaction
                {
                    TransactionID = Guid.NewGuid(), UserID = entity.StudentID, Type = WalletTxType.Refund,
                    Amount = refund, RelatedClassID = entity.ClassID,
                    Description = $"Hoàn ký quỹ dư lớp {entity.Name}", CreatedAt = now
                });
            }

            wallet.UpdatedAt = now;
            entity.EscrowAmount = newFee;
        }

        if (request.Fee is not null) entity.TuitionFee = request.Fee.Value;

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

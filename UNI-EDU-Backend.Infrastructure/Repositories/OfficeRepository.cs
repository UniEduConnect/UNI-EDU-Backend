using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Office;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class OfficeRepository(ApplicationDbContext dbContext) : IOfficeRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<(List<AttendanceResponse> Items, int Total)> GetAttendanceAsync(string? statusWire, int page, int pageSize, CancellationToken cancellationToken)
    {
        var q = _dbContext.Sessions.AsNoTracking().AsQueryable();

        var statuses = WireToStatuses(statusWire);
        if (statuses is not null)
            q = q.Where(s => statuses.Contains(s.Status));

        var total = await q.CountAsync(cancellationToken);

        var rows = await q
            .OrderByDescending(s => s.StartAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AttendanceProjection)
            .ToListAsync(cancellationToken);

        return (rows.Select(MapAttendance).ToList(), total);
    }

    public async Task<AttendanceResponse?> GetAttendanceByIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var row = await _dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.SessionID == sessionId)
            .Select(AttendanceProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : MapAttendance(row);
    }

    public async Task<bool> ConfirmAttendanceAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.SessionID == sessionId, cancellationToken);
        if (session is null) return false;

        session.OfficeConfirmed = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> ClassExistsAsync(Guid classId, CancellationToken cancellationToken) =>
        _dbContext.Classes.AnyAsync(c => c.ClassID == classId, cancellationToken);

    public Task<bool> SessionBelongsToClassAsync(Guid sessionId, Guid classId, CancellationToken cancellationToken) =>
        _dbContext.Sessions.AnyAsync(s => s.SessionID == sessionId && s.ClassID == classId, cancellationToken);

    public async Task<(List<IncidentResponse> Items, int Total)> GetIncidentsAsync(IncidentStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var q = _dbContext.Incidents.AsNoTracking().AsQueryable();
        if (status is not null)
            q = q.Where(i => i.Status == status);

        var total = await q.CountAsync(cancellationToken);

        var rows = await q
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(IncidentProjection)
            .ToListAsync(cancellationToken);

        return (rows.Select(MapIncident).ToList(), total);
    }

    public async Task<IncidentResponse> CreateIncidentAsync(Guid classId, Guid? sessionId, Guid? reporterId, string reporterName, string reporterRole, string description, IncidentPriority priority, CancellationToken cancellationToken)
    {
        var entity = new Incident
        {
            IncidentID = Guid.NewGuid(),
            ClassID = classId,
            SessionID = sessionId,
            ReporterUserId = reporterId,
            ReporterName = reporterName,
            ReporterRole = reporterRole,
            Description = description,
            Priority = priority,
            Status = IncidentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Incidents.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var row = await _dbContext.Incidents
            .AsNoTracking()
            .Where(i => i.IncidentID == entity.IncidentID)
            .Select(IncidentProjection)
            .FirstAsync(cancellationToken);

        return MapIncident(row);
    }

    public async Task<IncidentReviewOutcome> SetIncidentStatusAsync(Guid incidentId, IncidentStatus status, string? resolution, CancellationToken cancellationToken)
    {
        var incident = await _dbContext.Incidents.FirstOrDefaultAsync(i => i.IncidentID == incidentId, cancellationToken);
        if (incident is null) return IncidentReviewOutcome.NotFound;

        incident.Status = status;
        if (status == IncidentStatus.Resolved)
        {
            incident.Resolution = resolution;
            incident.ResolvedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return IncidentReviewOutcome.Done;
    }

    public async Task<(List<AppointmentResponse> Items, int Total)> GetAppointmentsAsync(AppointmentStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var q = _dbContext.Appointments.AsNoTracking().AsQueryable();
        if (status is not null) q = q.Where(a => a.Status == status);

        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .OrderByDescending(a => a.ScheduledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (rows.Select(MapAppointment).ToList(), total);
    }

    public async Task<AppointmentResponse> CreateAppointmentAsync(SaveAppointmentRequest request, CancellationToken cancellationToken)
    {
        var entity = new Appointment
        {
            AppointmentID = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description,
            WithName = request.WithName,
            WithUserId = request.WithUserId,
            ScheduledAt = DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Utc),
            Status = AppointmentStatus.Scheduled,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Appointments.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapAppointment(entity);
    }

    public async Task<AppointmentResponse?> UpdateAppointmentAsync(Guid id, SaveAppointmentRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Appointments.FirstOrDefaultAsync(a => a.AppointmentID == id, cancellationToken);
        if (entity is null) return null;

        entity.Title = request.Title.Trim();
        entity.Description = request.Description;
        entity.WithName = request.WithName;
        entity.WithUserId = request.WithUserId;
        entity.ScheduledAt = DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Utc);
        entity.Notes = request.Notes;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapAppointment(entity);
    }

    public async Task<bool> SetAppointmentStatusAsync(Guid id, AppointmentStatus status, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Appointments.FirstOrDefaultAsync(a => a.AppointmentID == id, cancellationToken);
        if (entity is null) return false;

        entity.Status = status;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static AppointmentResponse MapAppointment(Appointment a) => new()
    {
        Id = a.AppointmentID,
        Title = a.Title,
        Description = a.Description,
        WithName = a.WithName,
        WithUserId = a.WithUserId,
        ScheduledAt = a.ScheduledAt,
        Status = a.Status.ToString().ToLowerInvariant(),
        Notes = a.Notes,
        CreatedAt = a.CreatedAt
    };

    private static List<SessionStatus>? WireToStatuses(string? wire) =>
        (wire ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "upcoming" => [SessionStatus.Scheduled, SessionStatus.InProgress],
            "pending" => [SessionStatus.PendingConfirm],
            "completed" => [SessionStatus.Completed],
            "reported" => [SessionStatus.Missed, SessionStatus.Cancelled],
            _ => null
        };

    private static string StatusToWire(SessionStatus status) => status switch
    {
        SessionStatus.Scheduled or SessionStatus.InProgress => "upcoming",
        SessionStatus.PendingConfirm => "pending",
        SessionStatus.Completed => "completed",
        SessionStatus.Missed or SessionStatus.Cancelled => "reported",
        _ => status.ToString().ToLowerInvariant()
    };

    // Raw rows (enums intact) before in-memory wire mapping.
    private record AttendanceRow(Guid Id, Guid ClassId, string? ClassName, string? Tutor, string? Student, DateTime StartAt, SessionStatus Status, bool OfficeConfirmed);

    private static readonly System.Linq.Expressions.Expression<Func<Session, AttendanceRow>> AttendanceProjection =
        s => new AttendanceRow(
            s.SessionID,
            s.ClassID,
            s.Class.Name,
            s.Class.Tutor.FullName ?? s.Class.Tutor.User.Fullname,
            s.Class.Student.FullName ?? s.Class.Student.User.Fullname,
            s.StartAt,
            s.Status,
            s.OfficeConfirmed);

    private static AttendanceResponse MapAttendance(AttendanceRow r) => new()
    {
        Id = r.Id,
        ClassId = r.ClassId,
        ClassName = r.ClassName ?? string.Empty,
        Tutor = r.Tutor ?? string.Empty,
        Student = r.Student ?? string.Empty,
        Date = r.StartAt.ToString("yyyy-MM-dd"),
        Time = r.StartAt.ToString("HH:mm"),
        Status = StatusToWire(r.Status),
        ParentConfirmed = r.Status == SessionStatus.Completed,
        OfficeConfirmed = r.OfficeConfirmed
    };

    private record IncidentRow(Guid Id, Guid ClassId, string? ClassName, Guid? SessionId, string ReporterName, string ReporterRole, string Description, IncidentPriority Priority, IncidentStatus Status, string? Resolution, DateTime CreatedAt, DateTime? ResolvedAt);

    private static readonly System.Linq.Expressions.Expression<Func<Incident, IncidentRow>> IncidentProjection =
        i => new IncidentRow(
            i.IncidentID,
            i.ClassID,
            i.Class.Name,
            i.SessionID,
            i.ReporterName,
            i.ReporterRole,
            i.Description,
            i.Priority,
            i.Status,
            i.Resolution,
            i.CreatedAt,
            i.ResolvedAt);

    private static IncidentResponse MapIncident(IncidentRow r) => new()
    {
        Id = r.Id,
        ClassId = r.ClassId,
        ClassName = r.ClassName ?? string.Empty,
        SessionId = r.SessionId,
        Reporter = r.ReporterName,
        ReporterRole = r.ReporterRole,
        Description = r.Description,
        Priority = r.Priority.ToString().ToLowerInvariant(),
        Status = r.Status.ToString().ToLowerInvariant(),
        Resolution = r.Resolution,
        CreatedAt = r.CreatedAt,
        ResolvedAt = r.ResolvedAt
    };
}

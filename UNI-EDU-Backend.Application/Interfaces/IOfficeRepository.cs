using UNI_EDU_Backend.Application.DTOs.Office;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IOfficeRepository
{
    Task<(List<AttendanceResponse> Items, int Total)> GetAttendanceAsync(string? statusWire, int page, int pageSize, CancellationToken cancellationToken);
    Task<AttendanceResponse?> GetAttendanceByIdAsync(Guid sessionId, CancellationToken cancellationToken);

    // Marks the office acknowledgement on a session. False if the session doesn't exist.
    Task<bool> ConfirmAttendanceAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<bool> ClassExistsAsync(Guid classId, CancellationToken cancellationToken);
    Task<bool> SessionBelongsToClassAsync(Guid sessionId, Guid classId, CancellationToken cancellationToken);

    Task<(List<IncidentResponse> Items, int Total)> GetIncidentsAsync(IncidentStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<IncidentResponse> CreateIncidentAsync(
        Guid classId, Guid? sessionId, Guid? reporterId, string reporterName, string reporterRole,
        string description, IncidentPriority priority, CancellationToken cancellationToken);

    // Transitions status (investigate/resolve). Returns NotFound if the incident is missing.
    Task<IncidentReviewOutcome> SetIncidentStatusAsync(Guid incidentId, IncidentStatus status, string? resolution, CancellationToken cancellationToken);

    // Appointments
    Task<(List<AppointmentResponse> Items, int Total)> GetAppointmentsAsync(AppointmentStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<AppointmentResponse> CreateAppointmentAsync(SaveAppointmentRequest request, CancellationToken cancellationToken);
    Task<AppointmentResponse?> UpdateAppointmentAsync(Guid id, SaveAppointmentRequest request, CancellationToken cancellationToken);
    Task<bool> SetAppointmentStatusAsync(Guid id, AppointmentStatus status, CancellationToken cancellationToken);

    // Room inventory + occupancy. Occupied = distinct offline/hybrid classes with an active
    // session overlapping [windowStart, windowEnd] (real demand for a physical room).
    Task<RoomInventoryResponse> GetRoomInventoryAsync(DateTime windowStart, DateTime windowEnd, CancellationToken cancellationToken);
}

using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Admin;
using UNI_EDU_Backend.Application.DTOs.Office;

namespace UNI_EDU_Backend.Application.Services.Office;

public interface IOfficeService
{
    // Registrations = accounts awaiting approval (pending tutors/teachers).
    Task<PagedResult<AdminUserResponse>> GetRegistrationsAsync(int page, CancellationToken cancellationToken);
    Task ApproveRegistrationAsync(Guid userId, Guid officeUserId, CancellationToken cancellationToken);
    Task RejectRegistrationAsync(Guid userId, Guid officeUserId, CancellationToken cancellationToken);

    // Appointments
    Task<PagedResult<AppointmentResponse>> GetAppointmentsAsync(AppointmentListQuery query, CancellationToken cancellationToken);
    Task<AppointmentResponse> CreateAppointmentAsync(SaveAppointmentRequest request, CancellationToken cancellationToken);
    Task<AppointmentResponse> UpdateAppointmentAsync(Guid id, SaveAppointmentRequest request, CancellationToken cancellationToken);
    Task CancelAppointmentAsync(Guid id, CancellationToken cancellationToken);
    Task CompleteAppointmentAsync(Guid id, CancellationToken cancellationToken);

    // Heuristic schedule suggestion.
    Task<AiScheduleResponse> GenerateScheduleAsync(AiScheduleRequest request, CancellationToken cancellationToken);

    Task<PagedResult<AttendanceResponse>> GetAttendanceAsync(AttendanceListQuery query, CancellationToken cancellationToken);
    Task<AttendanceResponse> GetAttendanceByIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task ConfirmAttendanceAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<PagedResult<IncidentResponse>> GetIncidentsAsync(IncidentListQuery query, CancellationToken cancellationToken);
    Task<IncidentResponse> CreateIncidentAsync(CreateIncidentRequest request, Guid reporterId, string reporterName, string reporterRole, CancellationToken cancellationToken);
    Task InvestigateIncidentAsync(Guid incidentId, CancellationToken cancellationToken);
    Task ResolveIncidentAsync(Guid incidentId, ResolveIncidentRequest request, CancellationToken cancellationToken);

    // Room inventory + free/occupied counts for the scheduling page.
    Task<RoomInventoryResponse> GetRoomInventoryAsync(DateTime windowStart, DateTime windowEnd, CancellationToken cancellationToken);
}

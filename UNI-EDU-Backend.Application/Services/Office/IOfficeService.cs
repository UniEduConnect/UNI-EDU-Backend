using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Office;

namespace UNI_EDU_Backend.Application.Services.Office;

public interface IOfficeService
{
    Task<PagedResult<AttendanceResponse>> GetAttendanceAsync(AttendanceListQuery query, CancellationToken cancellationToken);
    Task<AttendanceResponse> GetAttendanceByIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task ConfirmAttendanceAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<PagedResult<IncidentResponse>> GetIncidentsAsync(IncidentListQuery query, CancellationToken cancellationToken);
    Task<IncidentResponse> CreateIncidentAsync(CreateIncidentRequest request, Guid reporterId, string reporterName, string reporterRole, CancellationToken cancellationToken);
    Task InvestigateIncidentAsync(Guid incidentId, CancellationToken cancellationToken);
    Task ResolveIncidentAsync(Guid incidentId, ResolveIncidentRequest request, CancellationToken cancellationToken);
}

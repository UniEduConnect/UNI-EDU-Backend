using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.DTOs.Sessions;

namespace UNI_EDU_Backend.Application.Services.Sessions;

public interface ISessionService
{
    Task<List<SessionResponse>> GetClassSessionsAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken);

    // Tutor adds a teaching session to a class's schedule.
    Task<SessionResponse> CreateAsync(Guid classId, CreateSessionRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task<SessionResponse> StartAsync(Guid sessionId, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task<SessionResponse> EndAsync(Guid sessionId, EndSessionRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task<SessionResponse> ConfirmAsync(Guid sessionId, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    // Tutor/Admin marks a session done in one step (skips start → end → parent-confirm).
    Task<SessionResponse> CompleteAsync(Guid sessionId, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task<SessionResponse> RateAsync(Guid sessionId, RateSessionRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken);

    Task<SessionResponse> RequestAbsenceAsync(Guid sessionId, CreateAbsenceRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task<SessionResponse> ApproveAbsenceAsync(Guid sessionId, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task<SessionResponse> RejectAbsenceAsync(Guid sessionId, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
}

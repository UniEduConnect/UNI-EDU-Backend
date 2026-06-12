using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.DTOs.Sessions;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

// Lightweight authorization/state context for a single session — fetched before any mutation
// so the service can run role + state-machine checks without loading the full graph.
public record SessionAuthContext(
    Guid SessionId,
    Guid ClassId,
    Guid TutorId,
    Guid StudentId,
    SessionStatus Status,
    string? AbsenceRequestedBy,
    bool? AbsenceApproved);

// The two parties on a class, used to authorize the session list endpoint.
public record ClassAccess(Guid TutorId, Guid StudentId);

public interface ISessionRepository
{
    Task<bool> IsParentOfStudentAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken);

    // Owners of a class (tutor + student). Null when the class does not exist.
    Task<ClassAccess?> GetClassAccessAsync(Guid classId, CancellationToken cancellationToken);

    // Sessions for a class, ordered chronologically. Empty list when the class has none.
    Task<List<SessionResponse>> GetByClassIdAsync(Guid classId, CancellationToken cancellationToken);

    // Tutor adds a teaching session to a class (also bumps the class's planned session count).
    Task<SessionResponse> CreateAsync(Guid classId, DateTime startAt, DateTime endAt, UNI_EDU_Backend.Domain.Enums.ClassFormat format, CancellationToken cancellationToken);

    // Authorization/state snapshot for one session. Null when the session does not exist.
    Task<SessionAuthContext?> GetContextAsync(Guid sessionId, CancellationToken cancellationToken);

    // State transitions. Each loads the tracked entity, applies the change, and persists.
    // Callers are expected to have already authorized + validated the transition via GetContextAsync.
    Task<SessionResponse> StartAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<SessionResponse> EndAsync(Guid sessionId, EndSessionRequest request, CancellationToken cancellationToken);

    // Confirm: scheduled escrow release (per-session proportional) + completed-session bump,
    // all in one transaction. See SessionRepository for the wallet movements.
    Task<SessionResponse> ConfirmAsync(Guid sessionId, CancellationToken cancellationToken);

    // Rate: store the rating on the session and mirror it into the tutor's review aggregate.
    Task<SessionResponse> RateAsync(Guid sessionId, RateSessionRequest request, CancellationToken cancellationToken);

    Task<SessionResponse> RequestAbsenceAsync(Guid sessionId, CreateAbsenceRequest request, CancellationToken cancellationToken);
    Task<SessionResponse> ApproveAbsenceAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<SessionResponse> RejectAbsenceAsync(Guid sessionId, CancellationToken cancellationToken);
}

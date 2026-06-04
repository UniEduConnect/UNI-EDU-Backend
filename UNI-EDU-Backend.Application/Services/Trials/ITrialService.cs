using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Trials;

namespace UNI_EDU_Backend.Application.Services.Trials;

public interface ITrialService
{
    // Caller must be Student or Parent (enforced at the controller via [Authorize(Roles = ...)]).
    // For Student: studentId is derived from the caller; any body.StudentId is ignored.
    // For Parent : body.StudentId is required and must be one of the caller's children.
    Task<TrialResponse> CreateAsync(Guid callerUserId, string callerRole, CreateTrialRequest request, CancellationToken cancellationToken);

    // Role-scoped list (Tutor inbox, Student outbox, Parent across children, Admin everything).
    // Optional status filter. Paged. See ITrialRepository.GetMyTrialsAsync for scoping details.
    Task<PagedResult<TrialResponse>> GetMyTrialsAsync(TrialListQuery query, Guid callerUserId, string callerRole, CancellationToken cancellationToken);

    // Tutor-only state transitions. Caller must be the owning tutor (enforced inside the
    // service, defense-in-depth alongside the controller's [Authorize(Roles = "Tutor")]).
    // Both throw NotFoundException if the trial doesn't exist, ForbiddenAccessException
    // if the caller doesn't own it, and BadRequestException if the trial is no longer pending.
    Task<TrialResponse> AcceptAsync(Guid trialId, Guid callerTutorId, CancellationToken cancellationToken);
    Task<TrialResponse> RejectAsync(Guid trialId, Guid callerTutorId, RejectTrialRequest request, CancellationToken cancellationToken);

    // Student/Parent-only completion. Caller must own the trial:
    //   Student -> trial.StudentID == caller
    //   Parent  -> trial.Student.ParentID == caller (any of their children's trials)
    // Requires the trial to currently be Accepted. Throws NotFound / Forbidden / BadRequest
    // for the same conditions as accept/reject.
    Task<TrialResponse> CompleteAsync(Guid trialId, Guid callerUserId, string callerRole, CompleteTrialRequest request, CancellationToken cancellationToken);
}

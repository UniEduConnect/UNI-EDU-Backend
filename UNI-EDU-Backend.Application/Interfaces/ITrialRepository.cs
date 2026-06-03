using UNI_EDU_Backend.Application.DTOs.Trials;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface ITrialRepository
{
    // Inserts the row and returns the response projection (joined tutor/student/subject names)
    // in a single round-trip after save. Existence checks for the related entities live on the
    // caller (TrialService) so this method assumes valid FK values.
    Task<TrialResponse> CreateAsync(TrialBooking trial, CancellationToken cancellationToken);

    // Role-scoped list:
    //   Tutor   -> trials addressed to the caller (inbox)
    //   Student -> trials the caller initiated (outbox)
    //   Parent  -> trials for any of the caller's children (Student.ParentID == caller)
    //   Admin   -> every row
    // Optional status filter. Sorted by CreatedAt DESC. Paged.
    Task<(List<TrialResponse> Items, int Total)> GetMyTrialsAsync(
        Guid callerUserId, string callerRole, TrialStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    // Tracked load by id. Returns null if not found. Service uses this to enforce ownership
    // (TutorID == caller) and current-status guards before requesting a transition.
    Task<TrialBooking?> GetByIdAsync(Guid trialId, CancellationToken cancellationToken);

    // Applies a status transition to an already-loaded (tracked) trial entity:
    // sets Status, stamps ReviewedAt = UtcNow, and writes ReviewNote when supplied. Saves
    // and returns the freshly-projected TrialResponse. Caller has already validated
    // ownership and that the current status is transitionable.
    Task<TrialResponse> ApplyTransitionAsync(TrialBooking trial, TrialStatus newStatus, string? reviewNote, CancellationToken cancellationToken);

    // Completion is a distinct transition (different audience, different stored fields):
    // sets Status = Completed, stamps CompletedAt = UtcNow, writes Feedback/Rating when
    // supplied. Does NOT touch ReviewedAt/ReviewNote — those belong to the tutor's accept/reject.
    Task<TrialResponse> ApplyCompletionAsync(TrialBooking trial, string? feedback, double? rating, CancellationToken cancellationToken);
}

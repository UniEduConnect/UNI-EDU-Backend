using UNI_EDU_Backend.Application.DTOs.Parents;

namespace UNI_EDU_Backend.Application.Services.Parents;

public interface IParentChildLinkService
{
    // Parent side: request to link to a student by email (notifies the student to confirm).
    Task RequestLinkAsync(Guid parentId, LinkChildRequest request, CancellationToken cancellationToken);

    // Student side: list pending link requests addressed to them.
    Task<List<ParentLinkRequestResponse>> GetPendingForStudentAsync(Guid studentId, CancellationToken cancellationToken);

    // Student side: approve (sets ParentID) or reject a pending request; notifies the parent.
    Task RespondAsync(Guid studentId, Guid requestId, bool approve, CancellationToken cancellationToken);
}

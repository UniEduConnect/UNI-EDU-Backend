using UNI_EDU_Backend.Application.DTOs.Parents;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IParentChildLinkRepository
{
    // The user id of a Student-role account matching this email (case-insensitive), or null.
    Task<Guid?> GetStudentUserIdByEmailAsync(string email, CancellationToken cancellationToken);

    // The current ParentID of a student (null if not linked yet).
    Task<Guid?> GetCurrentParentOfStudentAsync(Guid studentId, CancellationToken cancellationToken);

    // True if this parent already has a pending request for this student.
    Task<bool> HasPendingAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken);

    Task CreateRequestAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken);

    Task<List<ParentLinkRequestResponse>> GetPendingForStudentAsync(Guid studentId, CancellationToken cancellationToken);

    // Approves/rejects a pending request the student owns. On approve, sets Student.ParentID.
    // Returns the parent's user id on success (for notifying them), or null if no matching pending request.
    Task<Guid?> RespondAsync(Guid requestId, Guid studentId, bool approve, CancellationToken cancellationToken);

    Task<string> GetParentNameAsync(Guid parentId, CancellationToken cancellationToken);
    Task<string> GetStudentNameAsync(Guid studentId, CancellationToken cancellationToken);
}

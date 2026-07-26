using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.ClassRequests;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IClassRequestRepository
{
    Task CreateAsync(Guid studentId, CreateClassRequestRequest request, CancellationToken cancellationToken);

    // Open posts a tutor can browse ("find students").
    Task<PagedResult<ClassRequestResponse>> GetOpenAsync(ClassRequestListQuery query, int pageSize, CancellationToken cancellationToken);

    // A student's own posts.
    Task<List<ClassRequestResponse>> GetMineAsync(Guid studentId, CancellationToken cancellationToken);

    Task<(string Status, Guid StudentId, Guid SubjectId)?> GetAcceptInfoAsync(Guid requestId, CancellationToken cancellationToken);

    // Assigns the tutor + materializes a Class; returns the new ClassID (for deep-linking).
    Task<Guid> AssignAsync(Guid requestId, Guid tutorId, CancellationToken cancellationToken);

    // Read-only pre-check mirroring AssignAsync: throws if the request can't be accepted (not open,
    // schedule clash, or the student's wallet can't cover the tuition) — WITHOUT writing anything or
    // consuming a test. Lets the UI gate the AI test before a tutor wastes it on an unacceptable class.
    Task EnsureAcceptableAsync(Guid tutorId, Guid requestId, CancellationToken cancellationToken);
}

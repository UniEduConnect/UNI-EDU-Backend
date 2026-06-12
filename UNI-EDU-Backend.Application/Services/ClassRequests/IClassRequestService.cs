using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.ClassRequests;

namespace UNI_EDU_Backend.Application.Services.ClassRequests;

public interface IClassRequestService
{
    Task CreateAsync(Guid studentId, CreateClassRequestRequest request, CancellationToken cancellationToken);
    // A parent posts on behalf of a linked child (request.StudentId names the child).
    Task CreateForParentAsync(Guid parentId, CreateClassRequestRequest request, CancellationToken cancellationToken);
    Task<PagedResult<ClassRequestResponse>> GetOpenAsync(ClassRequestListQuery query, CancellationToken cancellationToken);
    Task<List<ClassRequestResponse>> GetMineAsync(Guid studentId, CancellationToken cancellationToken);

    // Tutor accepts a request — gated on passing a tutor-test for this acceptance.
    Task AcceptAsync(Guid tutorId, Guid requestId, AcceptClassRequestRequest request, CancellationToken cancellationToken);
}

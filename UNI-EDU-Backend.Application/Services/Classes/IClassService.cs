using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Classes;

namespace UNI_EDU_Backend.Application.Services.Classes;

public interface IClassService
{
    public Task<ClassResponse> CreateClassAsync(CreateClassRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken);

    public Task<ClassDetailResponse> GetClassByIdAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken);

    public Task<PagedResult<ClassListItemResponse>> GetMyClassesAsync(ClassListQuery query, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
}

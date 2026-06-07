using UNI_EDU_Backend.Application.DTOs.Parents;

namespace UNI_EDU_Backend.Application.Services.Parents;

public interface IParentService
{
    Task<List<StudentSummaryResponse>> GetMyChildrenAsync(Guid parentId, CancellationToken cancellationToken);
}

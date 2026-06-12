using UNI_EDU_Backend.Application.DTOs.Classes;

namespace UNI_EDU_Backend.Application.Services.Materials;

public interface IMaterialService
{
    Task<List<MaterialResponse>> GetClassMaterialsAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task<MaterialResponse> AddMaterialAsync(Guid classId, CreateMaterialRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task DeleteMaterialAsync(Guid classId, Guid materialId, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
}

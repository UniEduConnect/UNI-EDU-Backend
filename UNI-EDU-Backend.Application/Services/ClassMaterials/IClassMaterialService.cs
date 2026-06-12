using UNI_EDU_Backend.Application.DTOs.Classes;

namespace UNI_EDU_Backend.Application.Services.ClassMaterials;

public interface IClassMaterialService
{
    Task<List<MaterialResponse>> GetMaterialsAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken);

    // Tutor of the class only.
    Task<MaterialResponse> AddMaterialAsync(Guid classId, CreateMaterialRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken);

    // Tutor of the class only.
    Task DeleteMaterialAsync(Guid classId, Guid materialId, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
}

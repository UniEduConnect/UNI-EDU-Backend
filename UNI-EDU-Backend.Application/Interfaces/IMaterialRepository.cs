using UNI_EDU_Backend.Application.DTOs.Classes;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IMaterialRepository
{
    Task<ClassAccess?> GetClassAccessAsync(Guid classId, CancellationToken cancellationToken);
    Task<bool> IsParentOfStudentAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken);

    Task<List<MaterialResponse>> GetByClassIdAsync(Guid classId, CancellationToken cancellationToken);
    Task<MaterialResponse> CreateAsync(Guid classId, CreateMaterialRequest request, CancellationToken cancellationToken);

    // Returns false when no material with that id exists on the given class.
    Task<bool> DeleteAsync(Guid classId, Guid materialId, CancellationToken cancellationToken);
}

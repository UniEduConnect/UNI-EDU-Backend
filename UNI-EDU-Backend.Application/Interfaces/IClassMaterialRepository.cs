using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IClassMaterialRepository
{
    // Owners of a class (tutor + student), used to authorize the read endpoint.
    // Null when the class does not exist. Reuses the ClassAccess record from ISessionRepository.
    Task<ClassAccess?> GetClassAccessAsync(Guid classId, CancellationToken cancellationToken);

    Task<bool> IsParentOfStudentAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken);

    // Materials for a class, newest upload first. Empty list when the class has none.
    Task<List<MaterialResponse>> GetByClassIdAsync(Guid classId, CancellationToken cancellationToken);

    Task<MaterialResponse> AddAsync(ClassMaterial material, CancellationToken cancellationToken);

    // Deletes the material scoped to its class. Returns false when no matching row exists.
    Task<bool> DeleteAsync(Guid classId, Guid materialId, CancellationToken cancellationToken);
}

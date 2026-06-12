using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Materials;

public class MaterialService(
    IMaterialRepository materialRepo,
    IValidator<CreateMaterialRequest> createValidator) : IMaterialService
{
    private readonly IMaterialRepository _materialRepo = materialRepo;
    private readonly IValidator<CreateMaterialRequest> _createValidator = createValidator;

    public async Task<List<MaterialResponse>> GetClassMaterialsAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        // Any class participant (tutor, student, the student's parent) or Admin may view materials.
        await EnsureClassParticipantAsync(classId, callerUserId, callerRole, cancellationToken);
        return await _materialRepo.GetByClassIdAsync(classId, cancellationToken);
    }

    public async Task<MaterialResponse> AddMaterialAsync(Guid classId, CreateMaterialRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var access = await RequireClassAsync(classId, cancellationToken);

        if (!IsTutorOrAdmin(access, callerUserId, callerRole))
            throw new ForbiddenAccessException("Only the class tutor or an Admin can add materials.");

        await _createValidator.EnsureValidAsync(request, cancellationToken);

        return await _materialRepo.CreateAsync(classId, request, cancellationToken);
    }

    public async Task DeleteMaterialAsync(Guid classId, Guid materialId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var access = await RequireClassAsync(classId, cancellationToken);

        if (!IsTutorOrAdmin(access, callerUserId, callerRole))
            throw new ForbiddenAccessException("Only the class tutor or an Admin can delete materials.");

        if (!await _materialRepo.DeleteAsync(classId, materialId, cancellationToken))
            throw new NotFoundException($"Material with id '{materialId}' not found on this class.");
    }

    private async Task<ClassAccess> RequireClassAsync(Guid classId, CancellationToken cancellationToken) =>
        await _materialRepo.GetClassAccessAsync(classId, cancellationToken)
            ?? throw new NotFoundException($"Class with id '{classId}' not found.");

    private async Task EnsureClassParticipantAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var access = await RequireClassAsync(classId, cancellationToken);
        var role = (callerRole ?? string.Empty).Trim();

        bool allowed = role switch
        {
            "Admin" => true,
            "Tutor" => access.TutorId == callerUserId,
            "Student" => access.StudentId == callerUserId,
            "Parent" => await _materialRepo.IsParentOfStudentAsync(callerUserId, access.StudentId, cancellationToken),
            _ => false
        };

        if (!allowed)
            throw new ForbiddenAccessException("You do not have access to this class.");
    }

    private static bool IsTutorOrAdmin(ClassAccess access, Guid callerUserId, string callerRole)
    {
        var role = (callerRole ?? string.Empty).Trim();
        return role == "Admin" || (role == "Tutor" && access.TutorId == callerUserId);
    }
}

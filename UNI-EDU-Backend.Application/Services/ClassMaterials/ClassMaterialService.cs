using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Services.ClassMaterials;

public class ClassMaterialService(
    IClassMaterialRepository materialRepo,
    IValidator<CreateMaterialRequest> createValidator) : IClassMaterialService
{
    private readonly IClassMaterialRepository _materialRepo = materialRepo;
    private readonly IValidator<CreateMaterialRequest> _createValidator = createValidator;

    public async Task<List<MaterialResponse>> GetMaterialsAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var access = await _materialRepo.GetClassAccessAsync(classId, cancellationToken)
            ?? throw new NotFoundException($"Class with id '{classId}' not found.");

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

        return await _materialRepo.GetByClassIdAsync(classId, cancellationToken);
    }

    public async Task<MaterialResponse> AddMaterialAsync(Guid classId, CreateMaterialRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var access = await _materialRepo.GetClassAccessAsync(classId, cancellationToken)
            ?? throw new NotFoundException($"Class with id '{classId}' not found.");

        if (!IsClassTutor(access, callerUserId, callerRole))
            throw new ForbiddenAccessException("Only the class tutor can add materials.");

        await _createValidator.EnsureValidAsync(request, cancellationToken);

        var material = new ClassMaterial
        {
            MaterialID = Guid.NewGuid(),
            ClassID = classId,
            Name = request.Name.Trim(),
            Type = request.Type.Trim().ToLowerInvariant(),
            Url = request.Url.Trim(),
            Size = request.Size,
            UploadedAt = DateTime.UtcNow
        };

        return await _materialRepo.AddAsync(material, cancellationToken);
    }

    public async Task DeleteMaterialAsync(Guid classId, Guid materialId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var access = await _materialRepo.GetClassAccessAsync(classId, cancellationToken)
            ?? throw new NotFoundException($"Class with id '{classId}' not found.");

        if (!IsClassTutor(access, callerUserId, callerRole))
            throw new ForbiddenAccessException("Only the class tutor can delete materials.");

        var deleted = await _materialRepo.DeleteAsync(classId, materialId, cancellationToken);
        if (!deleted)
            throw new NotFoundException($"Material with id '{materialId}' not found in class '{classId}'.");
    }

    private static bool IsClassTutor(ClassAccess access, Guid callerUserId, string callerRole) =>
        (callerRole ?? string.Empty).Trim() == "Tutor" && access.TutorId == callerUserId;
}

using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Classes;

public class ClassService(IClassRepository classRepo, IValidator<CreateClassRequest> createValidator) : IClassService
{
    private readonly IClassRepository _classRepo = classRepo;
    private readonly IValidator<CreateClassRequest> _createValidator = createValidator;

    public async Task<ClassResponse> CreateClassAsync(CreateClassRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var role = (callerRole ?? string.Empty).Trim();

        // Resolve the booking student from the caller's identity.
        // - Student: ignore body.StudentId, always book for self.
        // - Parent : body.StudentId required; must be one of caller's children.
        // - Admin  : pass-through, body.StudentId trusted as-is.
        // - else   : 403.
        switch (role)
        {
            case "Student":
                request.StudentId = callerUserId;
                break;

            case "Parent":
                if (request.StudentId == Guid.Empty)
                    throw new BadRequestException("StudentId is required when a Parent books a class.");
                if (!await _classRepo.IsParentOfStudentAsync(callerUserId, request.StudentId, cancellationToken))
                    throw new ForbiddenAccessException("You can only book classes for your own children.");
                break;

            case "Admin":
                break;

            default:
                throw new ForbiddenAccessException("Only Student, Parent, or Admin can create a class.");
        }

        await _createValidator.EnsureValidAsync(request, cancellationToken);

        if (!await _classRepo.TutorExistsAsync(request.TutorId, cancellationToken))
            throw new NotFoundException($"Tutor with id '{request.TutorId}' not found.");

        if (!await _classRepo.StudentExistsAsync(request.StudentId, cancellationToken))
            throw new NotFoundException($"Student with id '{request.StudentId}' not found.");

        return await _classRepo.CreateClassWithEscrowAsync(request, cancellationToken);
    }

    public async Task<ClassDetailResponse> GetClassByIdAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var detail = await _classRepo.GetByIdAsync(classId, cancellationToken)
            ?? throw new NotFoundException($"Class with id '{classId}' not found.");

        var role = (callerRole ?? string.Empty).Trim();

        // Admin sees all. Tutor/Student check their own ID. Parent needs an extra lookup
        // (Student.ParentID isn't surfaced on ClassDetailResponse to avoid leaking it to the client).
        Console.WriteLine(detail.TutorId);
        bool allowed = role switch
        {
            "Admin" => true,
            "Tutor" => detail.TutorId == callerUserId,
            "Student" => detail.StudentId == callerUserId,
            "Parent" => await _classRepo.IsParentOfStudentAsync(callerUserId, detail.StudentId, cancellationToken),
            _ => false
        };

        if (!allowed)
            throw new ForbiddenAccessException("You do not have access to this class.");

        return detail;
    }
}

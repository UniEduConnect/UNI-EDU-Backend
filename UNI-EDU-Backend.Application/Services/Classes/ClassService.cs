using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Services.Classes;

public class ClassService(
    IClassRepository classRepo,
    IValidator<CreateClassRequest> createValidator,
    IValidator<ClassListQuery> listValidator,
    IValidator<UpdateClassRequest> updateValidator) : IClassService
{
    private const int PageSize = 10;

    private readonly IClassRepository _classRepo = classRepo;
    private readonly IValidator<CreateClassRequest> _createValidator = createValidator;
    private readonly IValidator<ClassListQuery> _listValidator = listValidator;
    private readonly IValidator<UpdateClassRequest> _updateValidator = updateValidator;

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

    public async Task<PagedResult<ClassListItemResponse>> GetMyClassesAsync(ClassListQuery query, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        await _listValidator.EnsureValidAsync(query, cancellationToken);

        var role = (callerRole ?? string.Empty).Trim();
        if (role is not ("Tutor" or "Student" or "Parent" or "Admin"))
            throw new ForbiddenAccessException("Your role cannot list classes.");

        var status = ParseStatus(query.Status);

        var (items, total) = await _classRepo.GetMyClassesAsync(callerUserId, role, status, query.Page, PageSize, cancellationToken);

        return new PagedResult<ClassListItemResponse>
        {
            Items = items,
            Total = total,
            Page = query.Page,
            PageSize = PageSize
        };
    }

    public async Task<ClassDetailResponse> UpdateClassAsync(Guid classId, UpdateClassRequest request, CancellationToken cancellationToken)
    {
        // Admin-only: enforced by [Authorize(Roles = "Admin")] on the controller action.
        await _updateValidator.EnsureValidAsync(request, cancellationToken);

        // Validate referenced entities when reassigning tutor/student/subject.
        if (request.TutorId is not null && !await _classRepo.TutorExistsAsync(request.TutorId.Value, cancellationToken))
            throw new NotFoundException($"Tutor with id '{request.TutorId}' not found.");
        if (request.StudentId is not null && !await _classRepo.StudentExistsAsync(request.StudentId.Value, cancellationToken))
            throw new NotFoundException($"Student with id '{request.StudentId}' not found.");
        if (request.SubjectId is not null && await _classRepo.GetSubjectNameAsync(request.SubjectId.Value, cancellationToken) is null)
            throw new NotFoundException($"Subject with id '{request.SubjectId}' not found.");

        var found = await _classRepo.UpdatePartialAsync(classId, request, cancellationToken);
        if (!found)
            throw new NotFoundException($"Class with id '{classId}' not found.");

        return await _classRepo.GetByIdAsync(classId, cancellationToken)
            ?? throw new NotFoundException($"Class with id '{classId}' not found.");
    }

    // Validator already guarantees the value is in the allowed set (or empty); parse defensively.
    private static ClassStatus? ParseStatus(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return Enum.TryParse<ClassStatus>(raw.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : throw new BadRequestException($"Invalid status '{raw}'.");
    }
}

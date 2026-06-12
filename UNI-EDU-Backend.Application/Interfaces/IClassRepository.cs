using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IClassRepository
{
    Task<bool> TutorExistsAsync(Guid tutorId, CancellationToken cancellationToken);
    Task<bool> StudentExistsAsync(Guid studentId, CancellationToken cancellationToken);
    Task<string?> GetSubjectNameAsync(Guid subjectId, CancellationToken cancellationToken);

    Task<ClassResponse> CreateClassWithEscrowAsync(CreateClassRequest request, CancellationToken cancellationToken);

    Task<ClassDetailResponse?> GetByIdAsync(Guid classId, CancellationToken cancellationToken);

    // Applies only the supplied (non-null) fields to an existing class. Returns false if no
    // class with that id exists.
    Task<bool> UpdatePartialAsync(Guid classId, UpdateClassRequest request, CancellationToken cancellationToken);

    // Lists classes scoped to the caller's role: Tutor/Student by own id, Parent by their
    // children, Admin sees all. Optionally filtered by status. Paged.
    Task<(List<ClassListItemResponse> Items, int Total)> GetMyClassesAsync(
        Guid callerUserId, string callerRole, ClassStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    Task<bool> IsParentOfStudentAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken);
}

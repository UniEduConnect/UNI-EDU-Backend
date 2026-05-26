using UNI_EDU_Backend.Application.DTOs.Classes;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IClassRepository
{
    Task<bool> TutorExistsAsync(Guid tutorId, CancellationToken cancellationToken);
    Task<bool> StudentExistsAsync(Guid studentId, CancellationToken cancellationToken);
    Task<string?> GetSubjectNameAsync(Guid subjectId, CancellationToken cancellationToken);

    Task<ClassResponse> CreateClassWithEscrowAsync(CreateClassRequest request, CancellationToken cancellationToken);

    Task<ClassDetailResponse?> GetByIdAsync(Guid classId, CancellationToken cancellationToken);

    Task<bool> IsParentOfStudentAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken);
}

using UNI_EDU_Backend.Application.DTOs.Profile;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IProfileRepository
{
    Task<CurrentUserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);

    // Updates supplied (non-null) common User fields. False if the user doesn't exist.
    Task<bool> UpdateUserCommonAsync(Guid userId, string? fullname, string? phoneNumber, CancellationToken cancellationToken);

    Task<bool> TutorExistsAsync(Guid tutorId, CancellationToken cancellationToken);
    Task<List<Guid>> GetMissingSubjectIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
    Task<bool> UpdateTutorProfileAsync(Guid tutorId, UpdateTutorProfileRequest request, CancellationToken cancellationToken);

    Task<StudentProfileResponse?> GetStudentProfileAsync(Guid studentId, CancellationToken cancellationToken);
    Task<bool> UpdateStudentProfileAsync(Guid studentId, UpdateStudentProfileRequest request, CancellationToken cancellationToken);

    Task<ParentProfileResponse?> GetParentProfileAsync(Guid parentId, CancellationToken cancellationToken);
    Task<bool> UpdateParentProfileAsync(Guid parentId, UpdateParentProfileRequest request, CancellationToken cancellationToken);

    Task<List<ScheduleItemResponse>> GetMyScheduleAsync(Guid userId, string role, CancellationToken cancellationToken);
}

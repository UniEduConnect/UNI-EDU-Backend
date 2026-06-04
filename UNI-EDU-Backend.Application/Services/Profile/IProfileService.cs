using UNI_EDU_Backend.Application.DTOs.Profile;

namespace UNI_EDU_Backend.Application.Services.Profile;

public interface IProfileService
{
    Task<CurrentUserResponse> GetMeAsync(Guid userId, CancellationToken cancellationToken);
    Task<CurrentUserResponse> UpdateMeAsync(Guid userId, UpdateMeRequest request, CancellationToken cancellationToken);

    Task<CurrentUserResponse> UpdateMyTutorProfileAsync(Guid tutorId, UpdateTutorProfileRequest request, CancellationToken cancellationToken);

    Task<StudentProfileResponse> GetMyStudentProfileAsync(Guid studentId, CancellationToken cancellationToken);
    Task<StudentProfileResponse> UpdateMyStudentProfileAsync(Guid studentId, UpdateStudentProfileRequest request, CancellationToken cancellationToken);

    Task<ParentProfileResponse> GetMyParentProfileAsync(Guid parentId, CancellationToken cancellationToken);
    Task<ParentProfileResponse> UpdateMyParentProfileAsync(Guid parentId, UpdateParentProfileRequest request, CancellationToken cancellationToken);

    Task<List<ScheduleItemResponse>> GetMyScheduleAsync(Guid userId, string role, CancellationToken cancellationToken);
}

using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.DTOs.Profile;
using UNI_EDU_Backend.Application.DTOs.Tutors;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IProfileRepository
{
    // Student weekly availability (mirrors tutor availability). Get returns null if the student is missing.
    Task<List<AvailableSlotDto>?> GetStudentAvailabilityAsync(Guid studentId, CancellationToken cancellationToken);
    Task<bool> SaveStudentAvailabilityAsync(Guid studentId, List<AvailableSlot> slots, CancellationToken cancellationToken);

    // All of the caller's sessions across their classes, optionally filtered by date window.
    Task<List<SessionResponse>> GetMySessionsAsync(Guid userId, string role, DateTime? from, DateTime? to, CancellationToken cancellationToken);

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

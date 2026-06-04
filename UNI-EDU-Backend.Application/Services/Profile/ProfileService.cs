using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Profile;
using UNI_EDU_Backend.Application.DTOs.Tutors;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Profile;

public class ProfileService(
    IProfileRepository profileRepo,
    IValidator<UpdateMeRequest> updateMeValidator,
    IValidator<UpdateTutorProfileRequest> tutorValidator,
    IValidator<UpdateStudentProfileRequest> studentValidator,
    IValidator<UpdateParentProfileRequest> parentValidator,
    IValidator<UpdateAvailabilityRequest> availabilityValidator) : IProfileService
{
    private readonly IProfileRepository _profileRepo = profileRepo;
    private readonly IValidator<UpdateMeRequest> _updateMeValidator = updateMeValidator;
    private readonly IValidator<UpdateTutorProfileRequest> _tutorValidator = tutorValidator;
    private readonly IValidator<UpdateStudentProfileRequest> _studentValidator = studentValidator;
    private readonly IValidator<UpdateParentProfileRequest> _parentValidator = parentValidator;
    private readonly IValidator<UpdateAvailabilityRequest> _availabilityValidator = availabilityValidator;

    public async Task<CurrentUserResponse> GetMeAsync(Guid userId, CancellationToken cancellationToken) =>
        await _profileRepo.GetCurrentUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Current user not found.");

    public async Task<CurrentUserResponse> UpdateMeAsync(Guid userId, UpdateMeRequest request, CancellationToken cancellationToken)
    {
        await _updateMeValidator.EnsureValidAsync(request, cancellationToken);

        if (!await _profileRepo.UpdateUserCommonAsync(userId, request.Fullname, request.PhoneNumber, cancellationToken))
            throw new NotFoundException("Current user not found.");

        return await GetMeAsync(userId, cancellationToken);
    }

    public async Task<CurrentUserResponse> UpdateMyTutorProfileAsync(Guid tutorId, UpdateTutorProfileRequest request, CancellationToken cancellationToken)
    {
        await _tutorValidator.EnsureValidAsync(request, cancellationToken);

        if (request.SubjectIds is { Count: > 0 })
        {
            var missing = await _profileRepo.GetMissingSubjectIdsAsync(request.SubjectIds, cancellationToken);
            if (missing.Count > 0)
                throw new BadRequestException($"These subject ids do not exist: {string.Join(", ", missing)}.");
        }

        if (!await _profileRepo.UpdateTutorProfileAsync(tutorId, request, cancellationToken))
            throw new NotFoundException("Tutor profile not found.");

        return await GetMeAsync(tutorId, cancellationToken);
    }

    public async Task<StudentProfileResponse> GetMyStudentProfileAsync(Guid studentId, CancellationToken cancellationToken) =>
        await _profileRepo.GetStudentProfileAsync(studentId, cancellationToken)
            ?? throw new NotFoundException("Student profile not found.");

    public async Task<StudentProfileResponse> UpdateMyStudentProfileAsync(Guid studentId, UpdateStudentProfileRequest request, CancellationToken cancellationToken)
    {
        await _studentValidator.EnsureValidAsync(request, cancellationToken);

        if (!await _profileRepo.UpdateStudentProfileAsync(studentId, request, cancellationToken))
            throw new NotFoundException("Student profile not found.");

        return await GetMyStudentProfileAsync(studentId, cancellationToken);
    }

    public async Task<ParentProfileResponse> GetMyParentProfileAsync(Guid parentId, CancellationToken cancellationToken) =>
        await _profileRepo.GetParentProfileAsync(parentId, cancellationToken)
            ?? throw new NotFoundException("Parent profile not found.");

    public async Task<ParentProfileResponse> UpdateMyParentProfileAsync(Guid parentId, UpdateParentProfileRequest request, CancellationToken cancellationToken)
    {
        await _parentValidator.EnsureValidAsync(request, cancellationToken);

        if (!await _profileRepo.UpdateParentProfileAsync(parentId, request, cancellationToken))
            throw new NotFoundException("Parent profile not found.");

        return await GetMyParentProfileAsync(parentId, cancellationToken);
    }

    public Task<List<ScheduleItemResponse>> GetMyScheduleAsync(Guid userId, string role, CancellationToken cancellationToken) =>
        _profileRepo.GetMyScheduleAsync(userId, role, cancellationToken);

    public Task<List<DTOs.Classes.SessionResponse>> GetMySessionsAsync(Guid userId, string role, DateTime? from, DateTime? to, CancellationToken cancellationToken) =>
        _profileRepo.GetMySessionsAsync(userId, role, from, to, cancellationToken);

    public async Task<List<AvailableSlotDto>> GetMyStudentAvailabilityAsync(Guid studentId, CancellationToken cancellationToken) =>
        await _profileRepo.GetStudentAvailabilityAsync(studentId, cancellationToken)
            ?? throw new NotFoundException("Student profile not found.");

    public async Task<List<AvailableSlotDto>> UpdateMyStudentAvailabilityAsync(Guid studentId, UpdateAvailabilityRequest request, CancellationToken cancellationToken)
    {
        await _availabilityValidator.EnsureValidAsync(request, cancellationToken);

        var slots = request.Slots
            .Select(s => new Domain.Models.AvailableSlot { Day = s.Day.Trim(), Time = s.Time.Trim() })
            .ToList();

        if (!await _profileRepo.SaveStudentAvailabilityAsync(studentId, slots, cancellationToken))
            throw new NotFoundException("Student profile not found.");

        return request.Slots.Select(s => new AvailableSlotDto { Day = s.Day.Trim(), Time = s.Time.Trim() }).ToList();
    }
}

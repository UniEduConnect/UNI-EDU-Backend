using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Profile;
using UNI_EDU_Backend.Application.DTOs.Tutors;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Profile;

public class ProfileService(
    IProfileRepository profileRepo,
    ITutorRepository tutorRepo,
    IAiCompletionService aiCompletion,
    IValidator<UpdateMeRequest> updateMeValidator,
    IValidator<UpdateTutorProfileRequest> tutorValidator,
    IValidator<UpdateStudentProfileRequest> studentValidator,
    IValidator<UpdateParentProfileRequest> parentValidator,
    IValidator<UpdateAvailabilityRequest> availabilityValidator) : IProfileService
{
    private readonly IProfileRepository _profileRepo = profileRepo;
    private readonly ITutorRepository _tutorRepo = tutorRepo;
    private readonly IAiCompletionService _aiCompletion = aiCompletion;
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

    // Smart matching: weekly slots where BOTH the student and the tutor are free.
    public async Task<List<AvailableSlotDto>> GetCommonSlotsWithTutorAsync(Guid studentId, Guid tutorId, CancellationToken cancellationToken)
    {
        var studentSlots = await _profileRepo.GetStudentAvailabilityAsync(studentId, cancellationToken) ?? new();
        var tutorSlots = await _tutorRepo.GetAvailabilityAsync(tutorId, cancellationToken) ?? new();

        static string Norm(string? d) => (d?.Trim() == "CN") ? "Chủ nhật" : (d?.Trim() ?? string.Empty);

        var tutorSet = tutorSlots.Select(s => $"{Norm(s.Day)}|{s.Time?.Trim()}").ToHashSet();

        var seen = new HashSet<string>();
        var common = new List<AvailableSlotDto>();
        foreach (var s in studentSlots)
        {
            var key = $"{Norm(s.Day)}|{s.Time?.Trim()}";
            if (tutorSet.Contains(key) && seen.Add(key))
                common.Add(new AvailableSlotDto { Day = Norm(s.Day), Time = s.Time?.Trim() ?? string.Empty });
        }
        return common;
    }

    // AI-ranked study slots: takes the tutor∩student free slots and asks the AI to pick & rank
    // the best ones with a short reason. Falls back to the raw common slots if AI is unavailable.
    public async Task<List<AiSlotSuggestionDto>> GetAiRankedSlotsWithTutorAsync(Guid studentId, Guid tutorId, int? sessionsPerWeek, CancellationToken cancellationToken)
    {
        var common = await GetCommonSlotsWithTutorAsync(studentId, tutorId, cancellationToken);
        if (common.Count == 0) return new();

        // How many sessions/week to schedule — bounded by what's actually available in the overlap.
        var target = sessionsPerWeek is > 0
            ? Math.Min(sessionsPerWeek.Value, common.Count)
            : Math.Min(5, common.Count);

        var commonText = string.Join("; ", common.Select(s => $"{s.Day} {s.Time}"));
        var system = "Bạn là trợ lý sắp xếp lịch học. Chỉ trả về JSON hợp lệ, không thêm chữ nào khác.";
        var user =
            $"Các khung giờ mà CẢ gia sư và học sinh đều rảnh: {commonText}. " +
            $"Hãy chọn và xếp hạng ĐÚNG {target} buổi học/tuần tốt nhất (ưu tiên buổi tối các ngày trong tuần, " +
            "tránh quá khuya, giãn cách đều các buổi trong tuần). " +
            "Trả JSON đúng định dạng: {\"slots\":[{\"day\":\"Thứ 2\",\"time\":\"19:00-21:00\",\"reason\":\"lý do ngắn\",\"score\":90}]}. " +
            "score là số 0-100, reason bằng tiếng Việt ngắn gọn. Chỉ chọn trong danh sách khung giờ đã cho.";

        var ai = await _aiCompletion.CompleteJsonAsync<AiSlotEnvelope>(system, user, cancellationToken);
        if (ai?.Slots is { Count: > 0 })
        {
            static string Norm(string? d) => (d?.Trim() == "CN") ? "Chủ nhật" : (d?.Trim() ?? string.Empty);
            var commonSet = common.Select(s => $"{Norm(s.Day)}|{s.Time}").ToHashSet();
            var ranked = ai.Slots
                .Where(s => commonSet.Contains($"{Norm(s.Day)}|{s.Time?.Trim()}"))
                .Select(s => new AiSlotSuggestionDto
                {
                    Day = Norm(s.Day),
                    Time = s.Time?.Trim() ?? string.Empty,
                    Reason = string.IsNullOrWhiteSpace(s.Reason) ? "Khung giờ phù hợp" : s.Reason!.Trim(),
                    Score = Math.Clamp(s.Score, 0, 100),
                })
                .OrderByDescending(s => s.Score)
                .Take(target)
                .ToList();
            if (ranked.Count > 0) return ranked;
        }

        // Fallback (no AI key / parse failure): rank the common slots deterministically.
        return common.Take(target).Select((s, i) => new AiSlotSuggestionDto
        {
            Day = s.Day, Time = s.Time, Reason = "Cả gia sư và học sinh đều rảnh", Score = Math.Max(50, 90 - i * 5),
        }).ToList();
    }

    private sealed class AiSlotEnvelope
    {
        public List<AiSlotRow>? Slots { get; set; }
    }
    private sealed class AiSlotRow
    {
        public string? Day { get; set; }
        public string? Time { get; set; }
        public string? Reason { get; set; }
        public int Score { get; set; }
    }
}

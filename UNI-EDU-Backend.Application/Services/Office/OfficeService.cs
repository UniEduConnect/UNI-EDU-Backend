using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Admin;
using UNI_EDU_Backend.Application.DTOs.Office;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Services.Office;

public class OfficeService(
    IOfficeRepository officeRepo,
    IAdminRepository adminRepo,
    IValidator<CreateIncidentRequest> createIncidentValidator,
    IValidator<SaveAppointmentRequest> appointmentValidator) : IOfficeService
{
    private const int PageSize = 20;

    private readonly IOfficeRepository _officeRepo = officeRepo;
    private readonly IAdminRepository _adminRepo = adminRepo;
    private readonly IValidator<CreateIncidentRequest> _createIncidentValidator = createIncidentValidator;
    private readonly IValidator<SaveAppointmentRequest> _appointmentValidator = appointmentValidator;

    public async Task<PagedResult<AdminUserResponse>> GetRegistrationsAsync(int page, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        var (items, total) = await _adminRepo.GetUsersAsync(new AdminUserListQuery { Status = "pending", Page = page }, PageSize, cancellationToken);
        return new PagedResult<AdminUserResponse> { Items = items, Total = total, Page = page, PageSize = PageSize };
    }

    public async Task ApproveRegistrationAsync(Guid userId, Guid officeUserId, CancellationToken cancellationToken)
    {
        var user = await _adminRepo.SetUserStatusAsync(userId, UserStatus.Approved, cancellationToken)
            ?? throw new NotFoundException($"User with id '{userId}' not found.");
        await _adminRepo.AddAuditLogAsync(officeUserId, "Duyệt đăng ký", $"{user.Name} ({user.Role})", cancellationToken);
    }

    public async Task RejectRegistrationAsync(Guid userId, Guid officeUserId, CancellationToken cancellationToken)
    {
        var user = await _adminRepo.SetUserStatusAsync(userId, UserStatus.Rejected, cancellationToken)
            ?? throw new NotFoundException($"User with id '{userId}' not found.");
        await _adminRepo.AddAuditLogAsync(officeUserId, "Từ chối đăng ký", $"{user.Name} ({user.Role})", cancellationToken);
    }

    public async Task<PagedResult<AppointmentResponse>> GetAppointmentsAsync(AppointmentListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var (items, total) = await _officeRepo.GetAppointmentsAsync(ParseAppointmentStatus(query.Status), page, PageSize, cancellationToken);
        return new PagedResult<AppointmentResponse> { Items = items, Total = total, Page = page, PageSize = PageSize };
    }

    public async Task<AppointmentResponse> CreateAppointmentAsync(SaveAppointmentRequest request, CancellationToken cancellationToken)
    {
        await _appointmentValidator.EnsureValidAsync(request, cancellationToken);
        return await _officeRepo.CreateAppointmentAsync(request, cancellationToken);
    }

    public async Task<AppointmentResponse> UpdateAppointmentAsync(Guid id, SaveAppointmentRequest request, CancellationToken cancellationToken)
    {
        await _appointmentValidator.EnsureValidAsync(request, cancellationToken);
        return await _officeRepo.UpdateAppointmentAsync(id, request, cancellationToken)
            ?? throw new NotFoundException($"Appointment with id '{id}' not found.");
    }

    public async Task CancelAppointmentAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await _officeRepo.SetAppointmentStatusAsync(id, AppointmentStatus.Cancelled, cancellationToken))
            throw new NotFoundException($"Appointment with id '{id}' not found.");
    }

    public async Task CompleteAppointmentAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await _officeRepo.SetAppointmentStatusAsync(id, AppointmentStatus.Completed, cancellationToken))
            throw new NotFoundException($"Appointment with id '{id}' not found.");
    }

    public AiScheduleResponse GenerateSchedule(AiScheduleRequest request)
    {
        var days = (request.PreferredDays is { Count: > 0 })
            ? request.PreferredDays
            : ["Thứ 2", "Thứ 4", "Thứ 6"];
        var time = string.IsNullOrWhiteSpace(request.PreferredTime) ? "18:00-20:00" : request.PreferredTime.Trim();
        var count = request.SessionsPerWeek < 1 ? 1 : Math.Min(request.SessionsPerWeek, days.Count);

        var slots = days.Take(count).Select(d => new AvailableSlotDto { Day = d, Time = time }).ToList();

        return new AiScheduleResponse
        {
            Message = $"Đề xuất {slots.Count} buổi/tuần{(string.IsNullOrWhiteSpace(request.Subject) ? "" : $" cho môn {request.Subject}")} (gợi ý theo quy tắc, chưa dùng AI).",
            SuggestedSlots = slots
        };
    }

    private static AppointmentStatus? ParseAppointmentStatus(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "scheduled" => AppointmentStatus.Scheduled,
            "completed" => AppointmentStatus.Completed,
            "cancelled" => AppointmentStatus.Cancelled,
            _ => null
        };

    public async Task<PagedResult<AttendanceResponse>> GetAttendanceAsync(AttendanceListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var (items, total) = await _officeRepo.GetAttendanceAsync(query.Status, page, PageSize, cancellationToken);

        return new PagedResult<AttendanceResponse>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = PageSize
        };
    }

    public async Task<AttendanceResponse> GetAttendanceByIdAsync(Guid sessionId, CancellationToken cancellationToken) =>
        await _officeRepo.GetAttendanceByIdAsync(sessionId, cancellationToken)
            ?? throw new NotFoundException($"Session with id '{sessionId}' not found.");

    public async Task ConfirmAttendanceAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        if (!await _officeRepo.ConfirmAttendanceAsync(sessionId, cancellationToken))
            throw new NotFoundException($"Session with id '{sessionId}' not found.");
    }

    public async Task<PagedResult<IncidentResponse>> GetIncidentsAsync(IncidentListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var (items, total) = await _officeRepo.GetIncidentsAsync(ParseStatus(query.Status), page, PageSize, cancellationToken);

        return new PagedResult<IncidentResponse>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = PageSize
        };
    }

    public async Task<IncidentResponse> CreateIncidentAsync(CreateIncidentRequest request, Guid reporterId, string reporterName, string reporterRole, CancellationToken cancellationToken)
    {
        await _createIncidentValidator.EnsureValidAsync(request, cancellationToken);

        if (!await _officeRepo.ClassExistsAsync(request.ClassId, cancellationToken))
            throw new NotFoundException($"Class with id '{request.ClassId}' not found.");

        if (request.SessionId is Guid sessionId &&
            !await _officeRepo.SessionBelongsToClassAsync(sessionId, request.ClassId, cancellationToken))
            throw new BadRequestException("The session does not belong to the specified class.");

        return await _officeRepo.CreateIncidentAsync(
            request.ClassId, request.SessionId, reporterId, reporterName, reporterRole,
            request.Description, ParsePriority(request.Priority), cancellationToken);
    }

    public async Task InvestigateIncidentAsync(Guid incidentId, CancellationToken cancellationToken)
    {
        if (await _officeRepo.SetIncidentStatusAsync(incidentId, IncidentStatus.Investigating, null, cancellationToken) == IncidentReviewOutcome.NotFound)
            throw new NotFoundException($"Incident with id '{incidentId}' not found.");
    }

    public async Task ResolveIncidentAsync(Guid incidentId, ResolveIncidentRequest request, CancellationToken cancellationToken)
    {
        if (await _officeRepo.SetIncidentStatusAsync(incidentId, IncidentStatus.Resolved, request.Resolution, cancellationToken) == IncidentReviewOutcome.NotFound)
            throw new NotFoundException($"Incident with id '{incidentId}' not found.");
    }

    private static IncidentPriority ParsePriority(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "low" => IncidentPriority.Low,
            "high" => IncidentPriority.High,
            _ => IncidentPriority.Medium
        };

    private static IncidentStatus? ParseStatus(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "pending" => IncidentStatus.Pending,
            "investigating" => IncidentStatus.Investigating,
            "resolved" => IncidentStatus.Resolved,
            _ => null
        };
}

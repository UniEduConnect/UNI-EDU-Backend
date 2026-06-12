using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.DTOs.Sessions;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Services.Sessions;

public class SessionService(
    ISessionRepository sessionRepo,
    INotificationRepository notificationRepo,
    IValidator<EndSessionRequest> endValidator,
    IValidator<RateSessionRequest> rateValidator,
    IValidator<CreateSessionRequest> createValidator,
    IValidator<CreateAbsenceRequest> absenceValidator) : ISessionService
{
    private readonly ISessionRepository _sessionRepo = sessionRepo;
    private readonly INotificationRepository _notificationRepo = notificationRepo;
    private readonly IValidator<EndSessionRequest> _endValidator = endValidator;
    private readonly IValidator<RateSessionRequest> _rateValidator = rateValidator;
    private readonly IValidator<CreateSessionRequest> _createValidator = createValidator;
    private readonly IValidator<CreateAbsenceRequest> _absenceValidator = absenceValidator;

    public async Task<SessionResponse> CreateAsync(Guid classId, CreateSessionRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        var access = await _sessionRepo.GetClassAccessAsync(classId, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy lớp học '{classId}'.");

        var role = (callerRole ?? string.Empty).Trim();
        if (!(role == "Admin" || (role == "Tutor" && access.TutorId == callerUserId)))
            throw new ForbiddenAccessException("Chỉ gia sư của lớp mới có thể thêm buổi dạy.");

        var format = (request.Format ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "offline" => ClassFormat.Offline,
            "hybrid" => ClassFormat.Hybrid,
            _ => ClassFormat.Online,
        };

        return await _sessionRepo.CreateAsync(classId, request.StartAt, request.EndAt, format, cancellationToken);
    }

    public async Task<List<SessionResponse>> GetClassSessionsAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var access = await _sessionRepo.GetClassAccessAsync(classId, cancellationToken)
            ?? throw new NotFoundException($"Class with id '{classId}' not found.");

        var role = (callerRole ?? string.Empty).Trim();
        bool allowed = role switch
        {
            "Admin" => true,
            "Tutor" => access.TutorId == callerUserId,
            "Student" => access.StudentId == callerUserId,
            "Parent" => await _sessionRepo.IsParentOfStudentAsync(callerUserId, access.StudentId, cancellationToken),
            _ => false
        };

        if (!allowed)
            throw new ForbiddenAccessException("You do not have access to this class.");

        return await _sessionRepo.GetByClassIdAsync(classId, cancellationToken);
    }

    public async Task<SessionResponse> StartAsync(Guid sessionId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var ctx = await RequireContextAsync(sessionId, cancellationToken);

        if (!IsClassTutor(ctx, callerUserId, callerRole))
            throw new ForbiddenAccessException("Only the class tutor can start a session.");

        if (ctx.Status != SessionStatus.Scheduled)
            throw new BadRequestException($"Cannot start a session in '{ctx.Status}' state. It must be scheduled.");

        return await _sessionRepo.StartAsync(sessionId, cancellationToken);
    }

    public async Task<SessionResponse> EndAsync(Guid sessionId, EndSessionRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var ctx = await RequireContextAsync(sessionId, cancellationToken);

        if (!IsClassTutor(ctx, callerUserId, callerRole))
            throw new ForbiddenAccessException("Only the class tutor can end a session.");

        await _endValidator.EnsureValidAsync(request, cancellationToken);

        if (ctx.Status != SessionStatus.InProgress)
            throw new BadRequestException($"Cannot end a session in '{ctx.Status}' state. It must be in progress.");

        var result = await _sessionRepo.EndAsync(sessionId, request, cancellationToken);

        // Notify the student and their linked parent that the session finished and needs confirmation.
        await _notificationRepo.NotifyStudentSideAsync(
            sessionId,
            "Buổi học đã hoàn thành",
            "Gia sư đã kết thúc buổi học. Vui lòng vào xác nhận điểm danh.",
            "/parent/children",
            cancellationToken);

        return result;
    }

    public async Task<SessionResponse> ConfirmAsync(Guid sessionId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var ctx = await RequireContextAsync(sessionId, cancellationToken);

        if (!await IsStudentSideAsync(ctx, callerUserId, callerRole, cancellationToken))
            throw new ForbiddenAccessException("Only the student or their parent can confirm a session.");

        if (ctx.Status != SessionStatus.PendingConfirm)
            throw new BadRequestException($"Cannot confirm a session in '{ctx.Status}' state. It must be pending confirmation.");

        return await _sessionRepo.ConfirmAsync(sessionId, cancellationToken);
    }

    public async Task<SessionResponse> CompleteAsync(Guid sessionId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var ctx = await RequireContextAsync(sessionId, cancellationToken);

        var role = (callerRole ?? string.Empty).Trim();
        if (!IsClassTutor(ctx, callerUserId, callerRole) && role != "Admin")
            throw new ForbiddenAccessException("Chỉ gia sư của lớp mới có thể hoàn thành buổi học.");

        if (ctx.Status == SessionStatus.Completed)
            throw new BadRequestException("Buổi học này đã hoàn thành.");
        if (ctx.Status == SessionStatus.Cancelled)
            throw new BadRequestException("Không thể hoàn thành một buổi đã huỷ.");

        // Reuse the same completion logic as parent-confirm: set Completed, bump the
        // class's completed count, and do the proportional escrow release.
        return await _sessionRepo.ConfirmAsync(sessionId, cancellationToken);
    }

    public async Task<SessionResponse> RateAsync(Guid sessionId, RateSessionRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var ctx = await RequireContextAsync(sessionId, cancellationToken);

        var role = (callerRole ?? string.Empty).Trim();
        if (!(role == "Student" && ctx.StudentId == callerUserId))
            throw new ForbiddenAccessException("Only the student can rate a session.");

        await _rateValidator.EnsureValidAsync(request, cancellationToken);

        if (ctx.Status != SessionStatus.Completed)
            throw new BadRequestException($"Cannot rate a session in '{ctx.Status}' state. It must be completed.");

        return await _sessionRepo.RateAsync(sessionId, request, cancellationToken);
    }

    public async Task<SessionResponse> RequestAbsenceAsync(Guid sessionId, CreateAbsenceRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var ctx = await RequireContextAsync(sessionId, cancellationToken);

        await _absenceValidator.EnsureValidAsync(request, cancellationToken);

        var requestedBy = request.RequestedBy.Trim().ToLowerInvariant();
        if (requestedBy == "tutor" && !IsClassTutor(ctx, callerUserId, callerRole))
            throw new ForbiddenAccessException("Only the class tutor can report a tutor absence.");
        if (requestedBy == "student" && !await IsStudentSideAsync(ctx, callerUserId, callerRole, cancellationToken))
            throw new ForbiddenAccessException("Only the student or their parent can report a student absence.");

        if (ctx.Status is SessionStatus.Completed or SessionStatus.Cancelled)
            throw new BadRequestException($"Cannot request an absence for a '{ctx.Status}' session.");

        if (ctx.AbsenceRequestedBy is not null && ctx.AbsenceApproved is null)
            throw new BadRequestException("An absence request is already pending for this session.");

        var result = await _sessionRepo.RequestAbsenceAsync(sessionId, request, cancellationToken);

        // Notify the counter-party so they can confirm/decline the absence.
        if (requestedBy == "tutor")
            await _notificationRepo.NotifySessionPartiesAsync(
                sessionId,
                "Gia sư xin nghỉ buổi học",
                $"Gia sư xin nghỉ một buổi học. Lý do: {request.Reason}. Bấm để quyết định CHO PHÉP hay TỪ CHỐI cho vắng.",
                $"/student/schedule?absence={sessionId}",
                $"/parent/children?absence={sessionId}",
                cancellationToken);
        else
            await _notificationRepo.NotifyTutorAsync(
                sessionId,
                "Học sinh báo vắng buổi học",
                $"Học sinh/phụ huynh đã báo vắng một buổi học (lý do: {request.Reason}). Vui lòng vào xác nhận.",
                "/tutor/classes",
                cancellationToken);

        return result;
    }

    public async Task<SessionResponse> ApproveAbsenceAsync(Guid sessionId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var ctx = await RequireContextAsync(sessionId, cancellationToken);

        if (ctx.AbsenceRequestedBy is null)
            throw new BadRequestException("There is no absence request to approve for this session.");

        if (ctx.AbsenceApproved == true || ctx.Status == SessionStatus.Cancelled)
            throw new BadRequestException("This absence request has already been approved.");

        // The counter-party approves: a tutor-reported absence is approved by the student side,
        // and a student-reported absence is approved by the tutor.
        if (ctx.AbsenceRequestedBy == "tutor")
        {
            if (!await IsStudentSideAsync(ctx, callerUserId, callerRole, cancellationToken))
                throw new ForbiddenAccessException("Only the student or their parent can approve a tutor-reported absence.");
        }
        else
        {
            if (!IsClassTutor(ctx, callerUserId, callerRole))
                throw new ForbiddenAccessException("Only the class tutor can approve a student-reported absence.");
        }

        var result = await _sessionRepo.ApproveAbsenceAsync(sessionId, cancellationToken);

        // Notify the party that originally requested the absence that it was confirmed.
        if (ctx.AbsenceRequestedBy == "tutor")
            await _notificationRepo.NotifyTutorAsync(
                sessionId,
                "Báo vắng đã được xác nhận",
                "Yêu cầu báo vắng của bạn đã được phụ huynh/học sinh xác nhận.",
                "/tutor/classes",
                cancellationToken);
        else
            await _notificationRepo.NotifyStudentSideAsync(
                sessionId,
                "Báo vắng đã được xác nhận",
                "Yêu cầu báo vắng đã được gia sư xác nhận.",
                "/parent/children",
                cancellationToken);

        return result;
    }

    public async Task<SessionResponse> RejectAbsenceAsync(Guid sessionId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var ctx = await RequireContextAsync(sessionId, cancellationToken);

        if (ctx.AbsenceRequestedBy is null)
            throw new BadRequestException("There is no absence request to reject for this session.");

        if (ctx.AbsenceApproved == true || ctx.Status == SessionStatus.Cancelled)
            throw new BadRequestException("This absence request has already been processed.");

        // Same counter-party rule as approving.
        if (ctx.AbsenceRequestedBy == "tutor")
        {
            if (!await IsStudentSideAsync(ctx, callerUserId, callerRole, cancellationToken))
                throw new ForbiddenAccessException("Only the student or their parent can decline a tutor-reported absence.");
        }
        else
        {
            if (!IsClassTutor(ctx, callerUserId, callerRole))
                throw new ForbiddenAccessException("Only the class tutor can decline a student-reported absence.");
        }

        var result = await _sessionRepo.RejectAbsenceAsync(sessionId, cancellationToken);

        // Tell the requester their absence was declined and the session still stands.
        if (ctx.AbsenceRequestedBy == "tutor")
            await _notificationRepo.NotifyTutorAsync(
                sessionId,
                "Báo vắng bị từ chối",
                "Phụ huynh/học sinh đã từ chối cho vắng. Buổi học vẫn diễn ra như lịch.",
                "/tutor/classes",
                cancellationToken);
        else
            await _notificationRepo.NotifyStudentSideAsync(
                sessionId,
                "Báo vắng bị từ chối",
                "Gia sư đã từ chối cho vắng. Buổi học vẫn diễn ra như lịch.",
                "/parent/children",
                cancellationToken);

        return result;
    }

    private async Task<SessionAuthContext> RequireContextAsync(Guid sessionId, CancellationToken cancellationToken) =>
        await _sessionRepo.GetContextAsync(sessionId, cancellationToken)
            ?? throw new NotFoundException($"Session with id '{sessionId}' not found.");

    private static bool IsClassTutor(SessionAuthContext ctx, Guid callerUserId, string callerRole) =>
        (callerRole ?? string.Empty).Trim() == "Tutor" && ctx.TutorId == callerUserId;

    private async Task<bool> IsStudentSideAsync(SessionAuthContext ctx, Guid callerUserId, string callerRole, CancellationToken cancellationToken) =>
        (callerRole ?? string.Empty).Trim() switch
        {
            "Student" => ctx.StudentId == callerUserId,
            "Parent" => await _sessionRepo.IsParentOfStudentAsync(callerUserId, ctx.StudentId, cancellationToken),
            _ => false
        };
}

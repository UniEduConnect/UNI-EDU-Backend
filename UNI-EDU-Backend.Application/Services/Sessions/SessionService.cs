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
    IValidator<EndSessionRequest> endValidator,
    IValidator<RateSessionRequest> rateValidator,
    IValidator<CreateAbsenceRequest> absenceValidator) : ISessionService
{
    private readonly ISessionRepository _sessionRepo = sessionRepo;
    private readonly IValidator<EndSessionRequest> _endValidator = endValidator;
    private readonly IValidator<RateSessionRequest> _rateValidator = rateValidator;
    private readonly IValidator<CreateAbsenceRequest> _absenceValidator = absenceValidator;

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

        return await _sessionRepo.EndAsync(sessionId, request, cancellationToken);
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

        return await _sessionRepo.RequestAbsenceAsync(sessionId, request, cancellationToken);
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

        return await _sessionRepo.ApproveAbsenceAsync(sessionId, cancellationToken);
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

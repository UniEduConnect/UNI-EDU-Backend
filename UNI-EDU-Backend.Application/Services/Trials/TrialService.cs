using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Trials;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Services.Trials;

public class TrialService(
    ITrialRepository trialRepo,
    ITutorRepository tutorRepo,
    IClassRepository classRepo,
    IValidator<CreateTrialRequest> createValidator,
    IValidator<TrialListQuery> listValidator,
    IValidator<RejectTrialRequest> rejectValidator,
    IValidator<CompleteTrialRequest> completeValidator) : ITrialService
{
    private const int PageSize = 10;

    private readonly ITrialRepository _trialRepo = trialRepo;
    private readonly ITutorRepository _tutorRepo = tutorRepo;
    private readonly IClassRepository _classRepo = classRepo;
    private readonly IValidator<CreateTrialRequest> _createValidator = createValidator;
    private readonly IValidator<TrialListQuery> _listValidator = listValidator;
    private readonly IValidator<RejectTrialRequest> _rejectValidator = rejectValidator;
    private readonly IValidator<CompleteTrialRequest> _completeValidator = completeValidator;

    public async Task<TrialResponse> CreateAsync(Guid callerUserId, string callerRole, CreateTrialRequest request, CancellationToken cancellationToken)
    {
        var role = (callerRole ?? string.Empty).Trim();

        // Resolve the booking student + parent link from caller identity.
        // - Student: ignore body.StudentId, always trial-for-self, no ParentID.
        // - Parent : body.StudentId required; must be one of caller's children; stamp ParentID.
        // - else   : 403.
        Guid? parentId = null;

        switch (role)
        {
            case "Student":
                request.StudentId = callerUserId;
                break;

            case "Parent":
                if (request.StudentId is null || request.StudentId == Guid.Empty)
                    throw new BadRequestException("StudentId is required when a Parent requests a trial.");
                if (!await _classRepo.IsParentOfStudentAsync(callerUserId, request.StudentId.Value, cancellationToken))
                    throw new ForbiddenAccessException("You can only request trials for your own children.");
                parentId = callerUserId;
                break;

            default:
                throw new ForbiddenAccessException("Only Student or Parent can request a trial.");
        }

        await _createValidator.EnsureValidAsync(request, cancellationToken);

        if (!await _tutorRepo.ExistsAsync(request.TutorId, cancellationToken))
            throw new NotFoundException($"Tutor with id '{request.TutorId}' not found.");

        if (!await _classRepo.StudentExistsAsync(request.StudentId!.Value, cancellationToken))
            throw new NotFoundException($"Student with id '{request.StudentId}' not found.");

        var subjectName = await _classRepo.GetSubjectNameAsync(request.SubjectId, cancellationToken);
        if (subjectName is null)
            throw new NotFoundException($"Subject with id '{request.SubjectId}' not found.");

        var booking = new TrialBooking
        {
            TrialID = Guid.NewGuid(),
            TutorID = request.TutorId,
            StudentID = request.StudentId!.Value,
            ParentID = parentId,
            SubjectID = request.SubjectId,
            RequestedAt = DateTime.SpecifyKind(request.RequestedAt, DateTimeKind.Utc),
            Goals = NullIfBlank(request.Goals),
            CurrentLevel = NullIfBlank(request.CurrentLevel),
            Note = NullIfBlank(request.Note),
            Status = TrialStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        return await _trialRepo.CreateAsync(booking, cancellationToken);
    }

    public async Task<PagedResult<TrialResponse>> GetMyTrialsAsync(TrialListQuery query, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        await _listValidator.EnsureValidAsync(query, cancellationToken);

        var role = (callerRole ?? string.Empty).Trim();
        if (role is not ("Tutor" or "Student" or "Parent" or "Admin"))
            throw new ForbiddenAccessException("Your role cannot list trials.");

        var status = ParseStatus(query.Status);

        var (items, total) = await _trialRepo.GetMyTrialsAsync(callerUserId, role, status, query.Page, PageSize, cancellationToken);

        return new PagedResult<TrialResponse>
        {
            Items = items,
            Total = total,
            Page = query.Page,
            PageSize = PageSize
        };
    }

    public Task<TrialResponse> AcceptAsync(Guid trialId, Guid callerTutorId, CancellationToken cancellationToken) =>
        TransitionAsync(trialId, callerTutorId, TrialStatus.Accepted, reviewNote: null, cancellationToken);

    public async Task<TrialResponse> RejectAsync(Guid trialId, Guid callerTutorId, RejectTrialRequest request, CancellationToken cancellationToken)
    {
        await _rejectValidator.EnsureValidAsync(request, cancellationToken);
        return await TransitionAsync(trialId, callerTutorId, TrialStatus.Rejected, NullIfBlank(request.ReviewNote), cancellationToken);
    }

    private async Task<TrialResponse> TransitionAsync(Guid trialId, Guid callerTutorId, TrialStatus newStatus, string? reviewNote, CancellationToken cancellationToken)
    {
        var trial = await _trialRepo.GetByIdAsync(trialId, cancellationToken)
            ?? throw new NotFoundException($"Trial with id '{trialId}' not found.");

        if (trial.TutorID != callerTutorId)
            throw new ForbiddenAccessException("You can only respond to your own trial requests.");

        if (trial.Status != TrialStatus.Pending)
            throw new BadRequestException($"Trial is no longer pending (current status: {trial.Status}).");

        return await _trialRepo.ApplyTransitionAsync(trial, newStatus, reviewNote, cancellationToken);
    }

    public async Task<TrialResponse> CompleteAsync(Guid trialId, Guid callerUserId, string callerRole, CompleteTrialRequest request, CancellationToken cancellationToken)
    {
        await _completeValidator.EnsureValidAsync(request, cancellationToken);

        var role = (callerRole ?? string.Empty).Trim();
        if (role is not ("Student" or "Parent"))
            throw new ForbiddenAccessException("Only Student or Parent can complete a trial.");

        var trial = await _trialRepo.GetByIdAsync(trialId, cancellationToken)
            ?? throw new NotFoundException($"Trial with id '{trialId}' not found.");

        // Ownership: Student must be the booking student; Parent must be the booking
        // student's parent (covers both parent-initiated and child-initiated trials).
        var allowed = role switch
        {
            "Student" => trial.StudentID == callerUserId,
            "Parent" => await _classRepo.IsParentOfStudentAsync(callerUserId, trial.StudentID, cancellationToken),
            _ => false
        };

        if (!allowed)
            throw new ForbiddenAccessException("You can only complete trials for yourself or your children.");

        if (trial.Status != TrialStatus.Accepted)
            throw new BadRequestException($"Trial cannot be completed (current status: {trial.Status}).");

        return await _trialRepo.ApplyCompletionAsync(trial, NullIfBlank(request.Feedback), request.Rating, cancellationToken);
    }

    private static string? NullIfBlank(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();

    // Validator already guarantees the value is in the allowed set (or empty); parse defensively.
    private static TrialStatus? ParseStatus(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return Enum.TryParse<TrialStatus>(raw.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : throw new BadRequestException($"Invalid status '{raw}'.");
    }
}

using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Trials;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Services.Trials;

public class TrialService(
    ITrialRepository trialRepo,
    INotificationRepository notificationRepo,
    IValidator<CreateTrialRequest> createValidator) : ITrialService
{
    private const int PageSize = 10;

    private readonly ITrialRepository _trialRepo = trialRepo;
    private readonly INotificationRepository _notificationRepo = notificationRepo;
    private readonly IValidator<CreateTrialRequest> _createValidator = createValidator;

    public async Task<TrialResponse> CreateAsync(Guid tutorId, CreateTrialRequest request, Guid studentId, CancellationToken cancellationToken)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        if (!await _trialRepo.TutorExistsAsync(tutorId, cancellationToken))
            throw new NotFoundException($"Tutor with id '{tutorId}' not found.");

        if (request.SubjectId is Guid subjectId && !await _trialRepo.SubjectExistsAsync(subjectId, cancellationToken))
            throw new BadRequestException($"Subject with id '{subjectId}' does not exist.");

        var trial = await _trialRepo.CreateAsync(studentId, tutorId, request, cancellationToken);

        // Notify the tutor of the incoming request.
        await _notificationRepo.CreateAsync(
            tutorId,
            "Yêu cầu học thử mới",
            $"{trial.StudentName} muốn học thử vào {trial.Day} {trial.Time}.",
            "info", "/tutor/students", cancellationToken);

        return trial;
    }

    public async Task<PagedResult<TrialResponse>> GetMineAsync(TrialListQuery query, Guid studentId, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var (items, total) = await _trialRepo.GetByStudentAsync(studentId, ParseStatus(query.Status), page, PageSize, cancellationToken);
        return Page(items, total, page);
    }

    public async Task<PagedResult<TrialResponse>> GetIncomingAsync(TrialListQuery query, Guid tutorId, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var (items, total) = await _trialRepo.GetByTutorAsync(tutorId, ParseStatus(query.Status), page, PageSize, cancellationToken);
        return Page(items, total, page);
    }

    public async Task<TrialResponse> RespondAsync(Guid trialId, Guid tutorId, bool accept, CancellationToken cancellationToken)
    {
        var (outcome, trial) = await _trialRepo.RespondAsync(trialId, tutorId, accept, cancellationToken);

        switch (outcome)
        {
            case TrialReviewOutcome.NotFound:
                throw new NotFoundException($"Trial request with id '{trialId}' not found.");
            case TrialReviewOutcome.Forbidden:
                throw new ForbiddenAccessException("You can only respond to trial requests addressed to you.");
            case TrialReviewOutcome.AlreadyResponded:
                throw new BadRequestException("This trial request has already been responded to.");
        }

        // Notify the student of the tutor's decision.
        await _notificationRepo.CreateAsync(
            trial!.StudentId,
            accept ? "Yêu cầu học thử được chấp nhận" : "Yêu cầu học thử bị từ chối",
            accept
                ? $"{trial.TutorName} đã chấp nhận buổi học thử {trial.Day} {trial.Time}."
                : $"{trial.TutorName} đã từ chối yêu cầu học thử của bạn.",
            accept ? "success" : "warning", "/student/classes", cancellationToken);

        return trial;
    }

    private static PagedResult<TrialResponse> Page(List<TrialResponse> items, int total, int page) => new()
    {
        Items = items,
        Total = total,
        Page = page,
        PageSize = PageSize
    };

    private static TrialStatus? ParseStatus(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "pending" => TrialStatus.Pending,
            "accepted" => TrialStatus.Accepted,
            "declined" => TrialStatus.Declined,
            _ => null
        };
}

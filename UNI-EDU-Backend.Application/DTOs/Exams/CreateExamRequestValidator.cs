using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Exams;

public class CreateExamRequestValidator : AbstractValidator<CreateExamRequest>
{
    private static readonly string[] AllowedTypes = ["tutor-test", "student-test"];
    private static readonly string[] AllowedCreators = ["ai", "tutor", "admin"];
    private static readonly string[] AllowedStatuses = ["draft", "open", "closed", "hidden"];
    private static readonly string[] AllowedDifficulties = ["easy", "medium", "hard"];

    public CreateExamRequestValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty().WithMessage("SubjectId is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description).MaximumLength(2000);

        RuleFor(x => x.Duration)
            .GreaterThan(0).WithMessage("Duration must be greater than 0 minutes.");

        RuleFor(x => x.Type)
            .Must(t => AllowedTypes.Contains((t ?? string.Empty).Trim().ToLowerInvariant()))
            .WithMessage("Type must be one of: tutor-test, student-test.");

        RuleFor(x => x.CreatedBy)
            .Must(c => AllowedCreators.Contains((c ?? string.Empty).Trim().ToLowerInvariant()))
            .WithMessage("CreatedBy must be one of: ai, tutor, admin.");

        RuleFor(x => x.Status)
            .Must(s => AllowedStatuses.Contains((s ?? string.Empty).Trim().ToLowerInvariant()))
            .WithMessage("Status must be one of: draft, open, closed, hidden.");

        RuleFor(x => x.Difficulty)
            .Must(d => AllowedDifficulties.Contains((d ?? string.Empty).Trim().ToLowerInvariant()))
            .WithMessage("Difficulty must be one of: easy, medium, hard.");

        RuleFor(x => x.Fee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxAttemptsPerUser).GreaterThan(0);
        RuleFor(x => x.ScoreScale)
            .Must(s => s == 10 || s == 100).WithMessage("ScoreScale must be 10 or 100.");

        RuleFor(x => x)
            .Must(x => x.StartDate is null || x.EndDate is null || x.EndDate >= x.StartDate)
            .WithMessage("EndDate must be on or after StartDate.");
    }
}

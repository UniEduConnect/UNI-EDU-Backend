using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Trials;

public class CreateTrialRequestValidator : AbstractValidator<CreateTrialRequest>
{
    public CreateTrialRequestValidator()
    {
        RuleFor(x => x.TutorId)
            .NotEmpty().WithMessage("tutorId is required.");

        RuleFor(x => x.SubjectId)
            .NotEmpty().WithMessage("subjectId is required.");

        RuleFor(x => x.RequestedAt)
            .Must(d => d > DateTime.UtcNow)
            .WithMessage("requestedAt must be a future UTC datetime.");

        RuleFor(x => x.Goals)
            .MaximumLength(500).WithMessage("goals must be 500 characters or fewer.");

        RuleFor(x => x.CurrentLevel)
            .MaximumLength(100).WithMessage("currentLevel must be 100 characters or fewer.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("note must be 500 characters or fewer.");
    }
}

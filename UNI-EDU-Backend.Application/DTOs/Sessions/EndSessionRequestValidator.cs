using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Sessions;

public class EndSessionRequestValidator : AbstractValidator<EndSessionRequest>
{
    public EndSessionRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(2000).WithMessage("Content must be 2000 characters or fewer.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must be 2000 characters or fewer.");

        RuleFor(x => x.Homework)
            .MaximumLength(2000).WithMessage("Homework must be 2000 characters or fewer.");
    }
}

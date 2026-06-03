using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Trials;

public class CompleteTrialRequestValidator : AbstractValidator<CompleteTrialRequest>
{
    public CompleteTrialRequestValidator()
    {
        RuleFor(x => x.Feedback)
            .MaximumLength(1000).WithMessage("feedback must be 1000 characters or fewer.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1d, 5d).WithMessage("rating must be between 1 and 5.")
            .When(x => x.Rating.HasValue);
    }
}

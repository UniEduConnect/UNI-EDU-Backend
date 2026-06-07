using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Sessions;

public class RateSessionRequestValidator : AbstractValidator<RateSessionRequest>
{
    public RateSessionRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(1000).WithMessage("Comment must be 1000 characters or fewer.");
    }
}

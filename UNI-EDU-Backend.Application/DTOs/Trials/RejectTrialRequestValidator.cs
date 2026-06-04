using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Trials;

public class RejectTrialRequestValidator : AbstractValidator<RejectTrialRequest>
{
    public RejectTrialRequestValidator()
    {
        RuleFor(x => x.ReviewNote)
            .MaximumLength(500).WithMessage("reviewNote must be 500 characters or fewer.");
    }
}

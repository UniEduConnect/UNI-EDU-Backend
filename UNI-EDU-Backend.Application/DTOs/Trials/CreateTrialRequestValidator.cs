using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Trials;

public class CreateTrialRequestValidator : AbstractValidator<CreateTrialRequest>
{
    public CreateTrialRequestValidator()
    {
        RuleFor(x => x.Day).NotEmpty().WithMessage("Day is required.").MaximumLength(50);
        RuleFor(x => x.Time).NotEmpty().WithMessage("Time is required.").MaximumLength(50);
        RuleFor(x => x.Message).MaximumLength(1000);
    }
}

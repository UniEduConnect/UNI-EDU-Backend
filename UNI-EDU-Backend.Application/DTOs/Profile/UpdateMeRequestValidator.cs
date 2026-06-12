using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Profile;

public class UpdateMeRequestValidator : AbstractValidator<UpdateMeRequest>
{
    public UpdateMeRequestValidator()
    {
        When(x => x.Fullname is not null, () =>
            RuleFor(x => x.Fullname)
                .NotEmpty().WithMessage("Fullname must not be empty.")
                .MaximumLength(100));

        When(x => x.PhoneNumber is not null, () =>
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("PhoneNumber must not be empty.")
                .MaximumLength(20));
    }
}

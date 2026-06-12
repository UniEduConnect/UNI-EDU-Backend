using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Profile;

public class UpdateStudentProfileRequestValidator : AbstractValidator<UpdateStudentProfileRequest>
{
    public UpdateStudentProfileRequestValidator()
    {
        When(x => x.FullName is not null, () =>
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(100));

        RuleFor(x => x.School).MaximumLength(200);

        When(x => x.Grade is not null, () =>
            RuleFor(x => x.Grade!.Value).InclusiveBetween(1, 12));
    }
}

public class UpdateParentProfileRequestValidator : AbstractValidator<UpdateParentProfileRequest>
{
    public UpdateParentProfileRequestValidator()
    {
        When(x => x.FullName is not null, () =>
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(100));
    }
}

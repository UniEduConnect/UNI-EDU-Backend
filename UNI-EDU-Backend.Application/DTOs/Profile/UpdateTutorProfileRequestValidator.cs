using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Profile;

public class UpdateTutorProfileRequestValidator : AbstractValidator<UpdateTutorProfileRequest>
{
    private static readonly string[] AllowedTutorTypes = ["tutor", "teacher"];

    public UpdateTutorProfileRequestValidator()
    {
        RuleFor(x => x.Bio).MaximumLength(2000);
        RuleFor(x => x.AvatarUrl).MaximumLength(2000);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.School).MaximumLength(200);
        RuleFor(x => x.Degree).MaximumLength(200);
        RuleFor(x => x.Gender).MaximumLength(20);
        RuleFor(x => x.TeachingStyle).MaximumLength(1000);
        RuleFor(x => x.IntroVideoUrl).MaximumLength(2000);

        When(x => x.HourlyRate is not null, () =>
            RuleFor(x => x.HourlyRate!.Value).GreaterThanOrEqualTo(0));

        When(x => x.YearsExperience is not null, () =>
            RuleFor(x => x.YearsExperience!.Value).InclusiveBetween(0, 80));

        When(x => x.TutorType is not null, () =>
            RuleFor(x => x.TutorType)
                .Must(t => AllowedTutorTypes.Contains((t ?? string.Empty).Trim().ToLowerInvariant()))
                .WithMessage("TutorType must be one of: tutor, teacher."));
    }
}

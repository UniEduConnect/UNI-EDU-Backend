using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Tutors;

public class TutorSearchQueryValidator : AbstractValidator<TutorSearchQuery>
{
    private static readonly string[] AllowedTypes = { "all", "tutor", "teacher" };

    public TutorSearchQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page must be >= 1.");
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).WithMessage("MinPrice must be >= 0.");
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(x => x.MinPrice).WithMessage("MaxPrice must be >= MinPrice.");
        RuleFor(x => x.Type)
            .Must(t => AllowedTypes.Contains((t ?? string.Empty).ToLowerInvariant()))
            .WithMessage("Type must be one of: all, tutor, teacher.");
    }
}

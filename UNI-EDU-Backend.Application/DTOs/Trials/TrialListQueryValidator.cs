using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Trials;

public class TrialListQueryValidator : AbstractValidator<TrialListQuery>
{
    private static readonly string[] AllowedStatuses =
        ["pending", "accepted", "rejected", "completed"];

    public TrialListQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("page must be >= 1.");

        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || AllowedStatuses.Contains(s.Trim().ToLowerInvariant()))
            .WithMessage("status must be one of: pending, accepted, rejected, completed.");
    }
}

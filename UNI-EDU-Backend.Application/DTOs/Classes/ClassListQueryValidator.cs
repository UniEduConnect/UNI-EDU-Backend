using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Classes;

public class ClassListQueryValidator : AbstractValidator<ClassListQuery>
{
    private static readonly string[] AllowedStatuses =
        ["searching", "active", "completed", "paused", "cancelled"];

    public ClassListQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("page must be >= 1.");

        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || AllowedStatuses.Contains(s.Trim().ToLowerInvariant()))
            .WithMessage("status must be one of: searching, active, completed, paused, cancelled.");
    }
}

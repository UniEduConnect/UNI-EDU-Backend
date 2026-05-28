using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Classes;

public class UpdateClassRequestValidator : AbstractValidator<UpdateClassRequest>
{
    // Non-financial statuses only. completed/cancelled move escrow → dedicated settlement flow.
    private static readonly string[] AllowedStatuses = ["searching", "active", "paused"];

    public UpdateClassRequestValidator()
    {
        RuleFor(x => x)
            .Must(r => r.Name is not null || r.Status is not null)
            .WithMessage("Provide at least one field to update (name or status).");

        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name!)
                .NotEmpty().WithMessage("name cannot be empty.")
                .MaximumLength(200).WithMessage("name must be 200 characters or fewer.");
        });

        When(x => x.Status is not null, () =>
        {
            RuleFor(x => x.Status!)
                .Must(s => AllowedStatuses.Contains(s.Trim().ToLowerInvariant()))
                .WithMessage("status must be one of: searching, active, paused. Use the settlement flow for completed/cancelled.");
        });
    }
}

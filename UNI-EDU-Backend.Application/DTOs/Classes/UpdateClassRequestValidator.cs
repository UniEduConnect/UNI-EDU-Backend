using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Classes;

public class UpdateClassRequestValidator : AbstractValidator<UpdateClassRequest>
{
    private static readonly string[] AllowedStatuses = ["searching", "active", "paused", "completed", "cancelled"];
    private static readonly string[] AllowedFormats = ["online", "offline", "hybrid"];

    public UpdateClassRequestValidator()
    {
        RuleFor(x => x)
            .Must(r => r.Name is not null || r.Status is not null || r.SubjectId is not null
                       || r.TutorId is not null || r.StudentId is not null || r.Format is not null
                       || r.Fee is not null || r.TotalSessions is not null)
            .WithMessage("Provide at least one field to update.");

        When(x => x.Name is not null, () =>
            RuleFor(x => x.Name!).NotEmpty().WithMessage("name cannot be empty.")
                .MaximumLength(200).WithMessage("name must be 200 characters or fewer."));

        When(x => x.Status is not null, () =>
            RuleFor(x => x.Status!)
                .Must(s => AllowedStatuses.Contains(s.Trim().ToLowerInvariant()))
                .WithMessage("status must be one of: searching, active, paused, completed, cancelled."));

        When(x => x.Format is not null, () =>
            RuleFor(x => x.Format!)
                .Must(f => AllowedFormats.Contains(f.Trim().ToLowerInvariant()))
                .WithMessage("format must be one of: online, offline, hybrid."));

        When(x => x.Fee is not null, () =>
            RuleFor(x => x.Fee!.Value).GreaterThanOrEqualTo(0).WithMessage("fee must be >= 0."));

        When(x => x.TotalSessions is not null, () =>
            RuleFor(x => x.TotalSessions!.Value).GreaterThanOrEqualTo(0).WithMessage("totalSessions must be >= 0."));
    }
}

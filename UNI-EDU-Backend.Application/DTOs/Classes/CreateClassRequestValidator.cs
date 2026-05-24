using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Classes;

public class CreateClassRequestValidator : AbstractValidator<CreateClassRequest>
{
    private static readonly string[] AllowedFormats = { "online", "offline", "hybrid" };

    public CreateClassRequestValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty().WithMessage("StudentId is required.");
        RuleFor(x => x.TutorId).NotEmpty().WithMessage("TutorId is required.");
        RuleFor(x => x.SubjectId).NotEmpty().WithMessage("SubjectId is required.");
        RuleFor(x => x.TotalSessions).GreaterThan(0).WithMessage("TotalSessions must be > 0.");
        RuleFor(x => x.Fee).GreaterThan(0).WithMessage("Fee must be > 0.");
        RuleFor(x => x.StartDate).GreaterThan(DateTime.MinValue).WithMessage("StartDate is required.");
        RuleFor(x => x.Format)
            .Must(f => AllowedFormats.Contains((f ?? string.Empty).ToLowerInvariant()))
            .WithMessage("Format must be one of: online, offline, hybrid.");

        RuleFor(x => x.WeeklySlots)
            .NotEmpty().WithMessage("WeeklySlots must contain at least one entry.");

        RuleForEach(x => x.WeeklySlots).ChildRules(slot =>
        {
            slot.RuleFor(s => s.DayOfWeek).InclusiveBetween(0, 6).WithMessage("DayOfWeek must be 0..6 (Sunday=0).");
            slot.RuleFor(s => s.EndTime).GreaterThan(s => s.StartTime).WithMessage("EndTime must be after StartTime.");
        });
    }
}

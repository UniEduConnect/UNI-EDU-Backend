using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Sessions;

public class CreateAbsenceRequestValidator : AbstractValidator<CreateAbsenceRequest>
{
    private static readonly string[] AllowedParties = { "tutor", "student" };

    public CreateAbsenceRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(1000).WithMessage("Reason must be 1000 characters or fewer.");

        RuleFor(x => x.RequestedBy)
            .Must(p => AllowedParties.Contains((p ?? string.Empty).ToLowerInvariant()))
            .WithMessage("RequestedBy must be one of: tutor, student.");
    }
}

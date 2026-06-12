using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Office;

public class CreateIncidentRequestValidator : AbstractValidator<CreateIncidentRequest>
{
    private static readonly string[] AllowedPriorities = ["low", "medium", "high"];

    public CreateIncidentRequestValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty().WithMessage("ClassId is required.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000);

        RuleFor(x => x.Priority)
            .Must(p => AllowedPriorities.Contains((p ?? string.Empty).Trim().ToLowerInvariant()))
            .WithMessage("Priority must be one of: low, medium, high.");
    }
}

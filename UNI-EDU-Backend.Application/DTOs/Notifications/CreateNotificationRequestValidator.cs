using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Notifications;

public class CreateNotificationRequestValidator : AbstractValidator<CreateNotificationRequest>
{
    private static readonly string[] AllowedTypes = ["info", "warning", "success", "error"];

    public CreateNotificationRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Type)
            .Must(t => AllowedTypes.Contains((t ?? string.Empty).Trim().ToLowerInvariant()))
            .WithMessage("Type must be one of: info, warning, success, error.");
        RuleFor(x => x.Link).MaximumLength(2000);
    }
}

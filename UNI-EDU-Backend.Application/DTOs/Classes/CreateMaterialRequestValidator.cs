using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Classes;

public class CreateMaterialRequestValidator : AbstractValidator<CreateMaterialRequest>
{
    private static readonly string[] AllowedTypes = { "pdf", "doc", "image", "video", "link" };

    public CreateMaterialRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(255).WithMessage("Name must be 255 characters or fewer.");

        RuleFor(x => x.Type)
            .Must(t => AllowedTypes.Contains((t ?? string.Empty).ToLowerInvariant()))
            .WithMessage("Type must be one of: pdf, doc, image, video, link.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Url is required.")
            .MaximumLength(2048).WithMessage("Url must be 2048 characters or fewer.");
    }
}

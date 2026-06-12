using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Classes;

public class CreateMaterialRequestValidator : AbstractValidator<CreateMaterialRequest>
{
    private static readonly string[] AllowedTypes = ["pdf", "doc", "image", "video", "link"];

    public CreateMaterialRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Type)
            .Must(t => AllowedTypes.Contains((t ?? string.Empty).Trim().ToLowerInvariant()))
            .WithMessage("Type must be one of: pdf, doc, image, video, link.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Url is required.")
            .MaximumLength(2000);

        RuleFor(x => x.Size).MaximumLength(50);
    }
}

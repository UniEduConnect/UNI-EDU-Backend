using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Questions;

public class UpdateQuestionRequestValidator : AbstractValidator<UpdateQuestionRequest>
{
    private static readonly string[] AllowedTypes = ["multiple-choice", "essay"];
    private static readonly string[] AllowedDifficulties = ["easy", "medium", "hard"];

    public UpdateQuestionRequestValidator()
    {
        RuleFor(x => x.SubjectId)
            .NotEmpty().WithMessage("SubjectId is required.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(2000).WithMessage("Content must be 2000 characters or fewer.");

        RuleFor(x => x.Type)
            .Must(t => AllowedTypes.Contains((t ?? string.Empty).Trim().ToLowerInvariant()))
            .WithMessage("Type must be one of: multiple-choice, essay.");

        RuleFor(x => x.Difficulty)
            .Must(d => AllowedDifficulties.Contains((d ?? string.Empty).Trim().ToLowerInvariant()))
            .WithMessage("Difficulty must be one of: easy, medium, hard.");

        When(x => (x.Type ?? string.Empty).Trim().ToLowerInvariant() != "essay", () =>
        {
            RuleFor(x => x.Options)
                .NotNull().Must(o => o.Count == 4).WithMessage("Multiple-choice questions must have exactly 4 options.");

            RuleForEach(x => x.Options)
                .NotEmpty().WithMessage("Option text must not be empty.");

            RuleFor(x => x.CorrectAnswer)
                .InclusiveBetween(0, 3).WithMessage("CorrectAnswer must be an index between 0 and 3.");
        });

        RuleFor(x => x.Topic).MaximumLength(200);
        RuleFor(x => x.Standard).MaximumLength(200);
    }
}

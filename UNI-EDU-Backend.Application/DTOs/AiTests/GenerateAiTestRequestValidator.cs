using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.AiTests;

public class GenerateAiTestRequestValidator : AbstractValidator<GenerateAiTestRequest>
{
    public GenerateAiTestRequestValidator()
    {
        RuleFor(x => x.SubjectId)
            .NotEmpty().WithMessage("SubjectId is required.");

        RuleFor(x => x.Grade)
            .InclusiveBetween(1, 12).WithMessage("Grade must be between 1 and 12.")
            .When(x => x.Grade.HasValue);

        RuleFor(x => x.Topic)
            .MaximumLength(120).WithMessage("Topic must not exceed 120 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Topic));
    }
}

using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Exams;

public class SubmitExamRequestValidator : AbstractValidator<SubmitExamRequest>
{
    public SubmitExamRequestValidator()
    {
        RuleFor(x => x.Answers)
            .NotNull().WithMessage("Answers is required.")
            .Must(a => a.Count > 0).WithMessage("At least one answer is required.");

        RuleForEach(x => x.Answers).ChildRules(a =>
        {
            a.RuleFor(x => x.QuestionId).GreaterThan(0);
            a.RuleFor(x => x.SelectedOption)
                .InclusiveBetween(0, 3).WithMessage("SelectedOption must be an index between 0 and 3.");
        });

        RuleFor(x => x.Answers)
            .Must(a => a.Select(x => x.QuestionId).Distinct().Count() == a.Count)
            .WithMessage("Answers must not contain duplicate QuestionId entries.")
            .When(x => x.Answers is not null);
    }
}

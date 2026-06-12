using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Exams;

public class SetExamQuestionsRequestValidator : AbstractValidator<SetExamQuestionsRequest>
{
    public SetExamQuestionsRequestValidator()
    {
        RuleFor(x => x.QuestionIds)
            .NotNull().WithMessage("QuestionIds is required.");

        RuleFor(x => x.QuestionIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("QuestionIds must not contain duplicates.")
            .When(x => x.QuestionIds is not null);
    }
}

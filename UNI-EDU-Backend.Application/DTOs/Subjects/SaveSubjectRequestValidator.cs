using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Subjects;

public class SaveSubjectRequestValidator : AbstractValidator<SaveSubjectRequest>
{
    public SaveSubjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);
    }
}

using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Office;

public class SaveAppointmentRequestValidator : AbstractValidator<SaveAppointmentRequest>
{
    public SaveAppointmentRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.").MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.WithName).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

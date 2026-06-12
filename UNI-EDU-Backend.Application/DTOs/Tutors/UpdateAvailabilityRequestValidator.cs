using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Tutors;

public class UpdateAvailabilityRequestValidator : AbstractValidator<UpdateAvailabilityRequest>
{
    public UpdateAvailabilityRequestValidator()
    {
        RuleFor(x => x.Slots)
            .NotNull().WithMessage("Slots is required.");

        RuleForEach(x => x.Slots).ChildRules(slot =>
        {
            slot.RuleFor(s => s.Day)
                .NotEmpty().WithMessage("Slot Day is required.")
                .MaximumLength(50);
            slot.RuleFor(s => s.Time)
                .NotEmpty().WithMessage("Slot Time is required.")
                .MaximumLength(50);
        });
    }
}

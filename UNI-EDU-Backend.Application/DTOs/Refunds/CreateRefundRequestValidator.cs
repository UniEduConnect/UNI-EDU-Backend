using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Refunds;

public class CreateRefundRequestValidator : AbstractValidator<CreateRefundRequest>
{
    public CreateRefundRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than 0.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Reason is required.").MaximumLength(1000);
    }
}

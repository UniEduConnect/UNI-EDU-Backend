using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Wallets;

public class DepositTestRequestValidator : AbstractValidator<DepositTestRequest>
{
    // Mirror the real deposit's bounds so the test path produces the same shape of ledger row.
    private const decimal MinAmount = 10_000m;
    private const decimal MaxAmount = 50_000_000m;

    public DepositTestRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(MinAmount).WithMessage($"amount must be at least {MinAmount:N0} VND.")
            .LessThanOrEqualTo(MaxAmount).WithMessage($"amount must not exceed {MaxAmount:N0} VND.");

        When(x => x.Note is not null, () =>
        {
            RuleFor(x => x.Note!).MaximumLength(200);
        });
    }
}

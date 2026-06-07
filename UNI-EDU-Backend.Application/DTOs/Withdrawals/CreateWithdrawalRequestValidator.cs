using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Withdrawals;

public class CreateWithdrawalRequestValidator : AbstractValidator<CreateWithdrawalRequest>
{
    private const decimal MinAmount = 50_000m;
    private const decimal MaxAmount = 50_000_000m;

    public CreateWithdrawalRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(MinAmount).WithMessage($"amount must be at least {MinAmount:N0} VND.")
            .LessThanOrEqualTo(MaxAmount).WithMessage($"amount must not exceed {MaxAmount:N0} VND per request.");

        RuleFor(x => x.Method)
            .Must(m => !string.IsNullOrWhiteSpace(m) && m.Trim().ToLowerInvariant() == "bank")
            .WithMessage("method must be 'bank' (momo/vnpay payouts are not supported yet).");

        // method == "bank" → bank info is required (only method allowed today).
        RuleFor(x => x.BankAccount)
            .NotEmpty().WithMessage("bankAccount is required.")
            .MaximumLength(50);

        RuleFor(x => x.BankName)
            .NotEmpty().WithMessage("bankName is required.")
            .MaximumLength(100);

        When(x => x.Note is not null, () =>
        {
            RuleFor(x => x.Note!).MaximumLength(500);
        });
    }
}

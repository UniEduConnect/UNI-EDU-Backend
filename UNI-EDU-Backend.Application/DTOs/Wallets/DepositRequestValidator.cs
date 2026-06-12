using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Wallets;

public class DepositRequestValidator : AbstractValidator<DepositRequest>
{
    private static readonly string[] AllowedMethods = ["momo", "vnpay", "bank", "payos"];

    // Momo sandbox accepts 1,000 .. 50,000,000 VND. Keep a sensible floor.
    private const decimal MinAmount = 10_000m;
    private const decimal MaxAmount = 50_000_000m;

    public DepositRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(MinAmount).WithMessage($"amount must be at least {MinAmount:N0} VND.")
            .LessThanOrEqualTo(MaxAmount).WithMessage($"amount must not exceed {MaxAmount:N0} VND.");

        RuleFor(x => x.Method)
            .Must(m => !string.IsNullOrWhiteSpace(m) && AllowedMethods.Contains(m.Trim().ToLowerInvariant()))
            .WithMessage("method must be one of: momo, vnpay, bank.");
    }
}

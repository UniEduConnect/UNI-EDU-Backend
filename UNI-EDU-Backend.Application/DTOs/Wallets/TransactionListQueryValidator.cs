using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Wallets;

public class TransactionListQueryValidator : AbstractValidator<TransactionListQuery>
{
    public TransactionListQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("page must be >= 1.");
    }
}

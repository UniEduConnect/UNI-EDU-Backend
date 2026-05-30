using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Tutors;

public class SaveBankAccountRequestValidator : AbstractValidator<SaveBankAccountRequest>
{
    // Letters (any Unicode — Vietnamese diacritics + Latin), digits (for business accounts
    // like "CONG TY TNHH 5"), spaces, and the punctuation that appears in real names:
    // . , & - / '   Anything else (@ # $ % *, etc.) is almost certainly garbage on this field.
    private const string AllowedHolderCharsPattern = @"^[\p{L}\p{Nd} .,&\-/']+$";

    public SaveBankAccountRequestValidator()
    {
        RuleFor(x => x.BankName)
            .NotEmpty().WithMessage("bankName is required.")
            .MaximumLength(100);

        RuleFor(x => x.BankAccount)
            .NotEmpty().WithMessage("bankAccount is required.")
            .MaximumLength(50);

        RuleFor(x => x.BankAccountHolder)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("bankAccountHolder is required.")
            .MinimumLength(2).WithMessage("bankAccountHolder must be at least 2 characters.")
            .MaximumLength(100).WithMessage("bankAccountHolder must be 100 characters or fewer.")
            .Matches(AllowedHolderCharsPattern)
                .WithMessage("bankAccountHolder may only contain letters, digits, spaces, and . , & - / '")
            .Must(s => s.Any(char.IsLetter))
                .WithMessage("bankAccountHolder must contain at least one letter.");
    }
}

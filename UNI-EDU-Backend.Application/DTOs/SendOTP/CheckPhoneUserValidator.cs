using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.SendOTP;

public class CheckPhoneUserValidator : AbstractValidator<CheckPhoneUserRequest>
{
    private const string VietnameseMobilePattern = @"^0\d{9}$";

    public CheckPhoneUserValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(VietnameseMobilePattern)
            .WithMessage("Phone number must be a valid Vietnamese mobile number (starts with 0 followed by 9 digits).");
    }
}

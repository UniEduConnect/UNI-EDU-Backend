using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.SendOTP;

public class CheckPhoneUserValidator : AbstractValidator<CheckPhoneUserRequest>
{
    public CheckPhoneUserValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.");
    }
}

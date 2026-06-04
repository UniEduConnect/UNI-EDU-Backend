using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Admin;

public class UpdateSystemSettingsRequestValidator : AbstractValidator<UpdateSystemSettingsRequest>
{
    public UpdateSystemSettingsRequestValidator()
    {
        RuleFor(x => x.PlatformName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EscrowPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.EscrowHoldDays).InclusiveBetween(0, 365);
        RuleFor(x => x.SessionTimeout).InclusiveBetween(1, 1440);
        RuleFor(x => x.MaxLoginAttempts).InclusiveBetween(1, 20);
    }
}

using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Chat;

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(4000).WithMessage("Message must be 4000 characters or fewer.");
    }
}

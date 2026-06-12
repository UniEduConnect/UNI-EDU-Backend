using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Sessions;

// A tutor adds a teaching session to a class's schedule.
public class CreateSessionRequest
{
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Format { get; set; } = "online"; // online | offline | hybrid
}

public class CreateSessionRequestValidator : AbstractValidator<CreateSessionRequest>
{
    public CreateSessionRequestValidator()
    {
        RuleFor(x => x.StartAt).NotEmpty().WithMessage("Vui lòng chọn thời gian bắt đầu.");
        RuleFor(x => x.EndAt).GreaterThan(x => x.StartAt).WithMessage("Giờ kết thúc phải sau giờ bắt đầu.");
    }
}

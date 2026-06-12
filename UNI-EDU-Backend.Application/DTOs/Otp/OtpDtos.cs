using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Otp;

public class SendOtpRequest
{
    public string Email { get; set; } = string.Empty;
    public string Purpose { get; set; } = "register";
}

public class VerifyOtpRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Purpose { get; set; } = "register";
}

public class SendOtpRequestValidator : AbstractValidator<SendOtpRequest>
{
    public SendOtpRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Email không hợp lệ.");
    }
}

public class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
{
    public VerifyOtpRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Email không hợp lệ.");
        RuleFor(x => x.Code).NotEmpty().Matches(@"^\d{6}$").WithMessage("Mã xác thực gồm 6 chữ số.");
    }
}

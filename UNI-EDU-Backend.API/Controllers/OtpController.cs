using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.Otp;
using UNI_EDU_Backend.Application.Services.Otp;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/otp")]
[ApiController]
public class OtpController(IEmailOtpService otpService) : ControllerBase
{
    private readonly IEmailOtpService _otpService = otpService;

    // Send a 6-digit verification code to the email (registration flow). Public.
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendOtpRequest request, CancellationToken cancellationToken)
    {
        await _otpService.SendOtpAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object?>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Đã gửi mã xác thực tới email của bạn.",
            Data = null
        });
    }

    // Verify a code. Throws (400) on wrong/expired/exhausted code. Public.
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        await _otpService.VerifyOtpAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Xác thực email thành công.",
            Data = new { verified = true }
        });
    }
}

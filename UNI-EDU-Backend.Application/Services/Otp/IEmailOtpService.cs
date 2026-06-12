using UNI_EDU_Backend.Application.DTOs.Otp;

namespace UNI_EDU_Backend.Application.Services.Otp;

public interface IEmailOtpService
{
    // Generate + email a one-time code (rate-limited). Always succeeds from the caller's view.
    Task SendOtpAsync(SendOtpRequest request, CancellationToken cancellationToken);

    // Verify a code. Throws BadRequestException on wrong/expired/exhausted code.
    Task VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken);

    // True if this email completed OTP verification recently (used to gate registration).
    Task<bool> IsEmailVerifiedAsync(string email, string purpose, CancellationToken cancellationToken);
}

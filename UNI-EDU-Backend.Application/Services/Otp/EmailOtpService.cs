using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.Extensions.Logging;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Otp;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Services.Otp;

public class EmailOtpService(
    IEmailOtpRepository repo,
    IEmailSender sender,
    IValidator<SendOtpRequest> sendValidator,
    IValidator<VerifyOtpRequest> verifyValidator,
    ILogger<EmailOtpService> logger) : IEmailOtpService
{
    private const int ExpiresMinutes = 5;
    private const int MaxAttempts = 5;
    private const int MaxPerHour = 5;
    private const int VerifiedWindowMinutes = 30; // a verified email stays "verified" this long for register

    private readonly IEmailOtpRepository _repo = repo;
    private readonly IEmailSender _sender = sender;
    private readonly IValidator<SendOtpRequest> _sendValidator = sendValidator;
    private readonly IValidator<VerifyOtpRequest> _verifyValidator = verifyValidator;
    private readonly ILogger<EmailOtpService> _logger = logger;

    public async Task SendOtpAsync(SendOtpRequest request, CancellationToken cancellationToken)
    {
        await _sendValidator.EnsureValidAsync(request, cancellationToken);

        var email = Normalize(request.Email);
        var purpose = Purpose(request.Purpose);

        var issued = await _repo.CountIssuedSinceAsync(email, DateTime.UtcNow.AddHours(-1), cancellationToken);
        if (issued >= MaxPerHour)
            throw new BadRequestException("Bạn đã yêu cầu quá nhiều mã xác thực. Vui lòng thử lại sau ít phút.");

        var code = GenerateCode();
        await _repo.AddAsync(new EmailOtp
        {
            Id = Guid.NewGuid(),
            Email = email,
            CodeHash = Hash(code),
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ExpiresMinutes),
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);

        var sent = await _sender.SendAsync(
            email, string.Empty,
            OtpEmailTemplate.Subject(code),
            OtpEmailTemplate.BuildHtml(code, ExpiresMinutes),
            OtpEmailTemplate.BuildText(code, ExpiresMinutes),
            cancellationToken);

        // When SMTP isn't configured (or send fails), surface the code in logs so the flow is
        // still testable in development. In production, configure SMTP so this branch never runs.
        if (!sent)
            _logger.LogWarning("OTP email to {Email} was not delivered. Code for dev/manual use: {Code}", email, code);
    }

    public async Task VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        await _verifyValidator.EnsureValidAsync(request, cancellationToken);

        var email = Normalize(request.Email);
        var purpose = Purpose(request.Purpose);

        var otp = await _repo.GetLatestActiveAsync(email, purpose, cancellationToken)
            ?? throw new BadRequestException("Mã xác thực không hợp lệ hoặc đã hết hạn.");

        if (otp.Attempts >= MaxAttempts)
            throw new BadRequestException("Bạn đã nhập sai quá nhiều lần. Vui lòng gửi lại mã mới.");

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(Hash(request.Code)),
                Encoding.UTF8.GetBytes(otp.CodeHash)))
        {
            otp.Attempts++;
            await _repo.UpdateAsync(otp, cancellationToken);
            throw new BadRequestException("Mã xác thực không đúng.");
        }

        otp.Consumed = true;
        otp.VerifiedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(otp, cancellationToken);
    }

    public Task<bool> IsEmailVerifiedAsync(string email, string purpose, CancellationToken cancellationToken) =>
        _repo.HasVerifiedSinceAsync(Normalize(email), Purpose(purpose), DateTime.UtcNow.AddMinutes(-VerifiedWindowMinutes), cancellationToken);

    private static string Normalize(string email) => (email ?? string.Empty).Trim().ToLowerInvariant();
    private static string Purpose(string? p) => string.IsNullOrWhiteSpace(p) ? "register" : p.Trim().ToLowerInvariant();
    private static string GenerateCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    private static string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code ?? string.Empty)));
}

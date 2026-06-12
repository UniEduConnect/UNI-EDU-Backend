using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IEmailOtpRepository
{
    Task AddAsync(EmailOtp otp, CancellationToken cancellationToken);
    Task UpdateAsync(EmailOtp otp, CancellationToken cancellationToken);

    // Latest still-usable code (not consumed, not expired) for an email+purpose.
    Task<EmailOtp?> GetLatestActiveAsync(string email, string purpose, CancellationToken cancellationToken);

    // How many codes were issued to this email since `since` (rate limiting).
    Task<int> CountIssuedSinceAsync(string email, DateTime since, CancellationToken cancellationToken);

    // True if this email has a successfully-verified code since `since` (register gate).
    Task<bool> HasVerifiedSinceAsync(string email, string purpose, DateTime since, CancellationToken cancellationToken);
}

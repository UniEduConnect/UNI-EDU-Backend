using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class EmailOtpRepository(ApplicationDbContext dbContext) : IEmailOtpRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task AddAsync(EmailOtp otp, CancellationToken cancellationToken)
    {
        _dbContext.EmailOtps.Add(otp);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(EmailOtp otp, CancellationToken cancellationToken)
    {
        _dbContext.EmailOtps.Update(otp);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<EmailOtp?> GetLatestActiveAsync(string email, string purpose, CancellationToken cancellationToken) =>
        _dbContext.EmailOtps.AsTracking()
            .Where(o => o.Email == email && o.Purpose == purpose && !o.Consumed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<int> CountIssuedSinceAsync(string email, DateTime since, CancellationToken cancellationToken) =>
        _dbContext.EmailOtps.AsNoTracking()
            .CountAsync(o => o.Email == email && o.CreatedAt >= since, cancellationToken);

    public Task<bool> HasVerifiedSinceAsync(string email, string purpose, DateTime since, CancellationToken cancellationToken) =>
        _dbContext.EmailOtps.AsNoTracking()
            .AnyAsync(o => o.Email == email && o.Purpose == purpose && o.VerifiedAt != null && o.VerifiedAt >= since, cancellationToken);
}

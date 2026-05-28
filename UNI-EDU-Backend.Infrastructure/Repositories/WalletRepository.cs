using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class WalletRepository(ApplicationDbContext dbContext) : IWalletRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public Task<decimal> GetBalanceAsync(Guid userId, CancellationToken cancellationToken) =>
        _dbContext.Wallets
            .AsNoTracking()
            .Where(w => w.UserID == userId)
            .Select(w => w.Balance)
            .FirstOrDefaultAsync(cancellationToken); // default 0 when no wallet row exists

    public async Task<decimal> GetTutorOutstandingEscrowAsync(Guid tutorId, CancellationToken cancellationToken) =>
        await _dbContext.Classes
            .AsNoTracking()
            .Where(c => c.TutorID == tutorId)
            // Cast to decimal? so an empty set sums to NULL → 0 instead of throwing.
            .SumAsync(c => (decimal?)(c.EscrowAmount - c.EscrowReleased), cancellationToken) ?? 0m;
}

using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Wallets;
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

    public async Task<(List<WalletTransactionRow> Items, int Total)> GetTransactionsAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _dbContext.WalletTransactions
            .AsNoTracking()
            .Where(t => t.UserID == userId);

        var total = await query.CountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.TransactionID)
            .Skip(skip)
            .Take(pageSize)
            .Select(t => new WalletTransactionRow(
                t.TransactionID,
                t.Type,
                t.Amount,
                t.Description,
                t.CreatedAt,
                t.RelatedClassID,
                // LEFT JOIN to the related class for the parent-view childId.
                t.Class != null ? t.Class.StudentID : null))
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}

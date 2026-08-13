using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Wallets;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

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
                t.Class != null ? t.Class.StudentID : null,
                t.Status,
                t.ReceiptUrl))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<Guid> CreatePendingDepositAsync(
        Guid userId, decimal amount, string method, string orderId, string description, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // WalletTransaction.UserID has an FK to Wallet — ensure the wallet row exists (balance 0).
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserID == userId, cancellationToken);
        if (wallet is null)
        {
            wallet = new Wallet { UserID = userId, Balance = 0m, EscrowBalance = 0m, UpdatedAt = now };
            _dbContext.Wallets.Add(wallet);
        }

        var tx = new WalletTransaction
        {
            TransactionID = Guid.NewGuid(),
            UserID = userId,
            Type = WalletTxType.Deposit,
            Status = WalletTxStatus.Pending,
            Amount = amount,
            Method = method,
            ProviderRef = orderId,
            Description = description,
            CreatedAt = now
        };
        _dbContext.WalletTransactions.Add(tx);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return tx.TransactionID;
    }

    public Task<TestDepositLookup?> LookupTestDepositAsync(Guid transactionId, CancellationToken cancellationToken) =>
        _dbContext.WalletTransactions
            .AsNoTracking()
            .Where(t => t.TransactionID == transactionId && t.Type == WalletTxType.Deposit)
            .Select(t => new TestDepositLookup(t.UserID, t.ProviderRef ?? string.Empty, t.Method, t.Status, t.Amount))
            .Cast<TestDepositLookup?>()
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<DepositSettleOutcome> SettleDepositAsync(
        string orderId, bool success, string providerTxnId, decimal confirmedAmount, string? receiptUrl, CancellationToken cancellationToken)
    {
        await using var dbTx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var tx = await _dbContext.WalletTransactions
            .FirstOrDefaultAsync(t => t.ProviderRef == orderId && t.Type == WalletTxType.Deposit, cancellationToken);

        if (tx is null)
            return DepositSettleOutcome.NotFound;

        // Idempotency: only a still-Pending deposit may transition. Retries are no-ops.
        if (tx.Status != WalletTxStatus.Pending)
            return DepositSettleOutcome.AlreadySettled;

        tx.ProviderTxnId = providerTxnId;
        tx.ReceiptUrl = receiptUrl;

        if (!success)
        {
            tx.Status = WalletTxStatus.Failed;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await dbTx.CommitAsync(cancellationToken);
            return DepositSettleOutcome.Failed;
        }

        // Trust the provider's confirmed amount — never credit if it disagrees with what we recorded.
        if (tx.Amount != confirmedAmount)
        {
            tx.Status = WalletTxStatus.Failed;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await dbTx.CommitAsync(cancellationToken);
            return DepositSettleOutcome.AmountMismatch;
        }

        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserID == tx.UserID, cancellationToken);
        if (wallet is null)
        {
            wallet = new Wallet { UserID = tx.UserID, Balance = 0m, EscrowBalance = 0m, UpdatedAt = DateTime.UtcNow };
            _dbContext.Wallets.Add(wallet);
        }

        wallet.Balance += confirmedAmount;
        wallet.UpdatedAt = DateTime.UtcNow;
        tx.Status = WalletTxStatus.Completed;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await dbTx.CommitAsync(cancellationToken);
        return DepositSettleOutcome.Credited;
    }

    public async Task<bool> TransferAsync(Guid fromUserId, Guid toUserId, decimal amount, string fromDescription, string toDescription, CancellationToken cancellationToken)
    {
        await using var dbTx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var fromWallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserID == fromUserId, cancellationToken);
        if (fromWallet is null || fromWallet.Balance < amount)
            return false; // no source wallet / insufficient balance

        var toWallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserID == toUserId, cancellationToken);
        if (toWallet is null)
        {
            toWallet = new Wallet { UserID = toUserId, Balance = 0m, EscrowBalance = 0m, UpdatedAt = now };
            _dbContext.Wallets.Add(toWallet);
        }

        fromWallet.Balance -= amount;
        fromWallet.UpdatedAt = now;
        toWallet.Balance += amount;
        toWallet.UpdatedAt = now;

        _dbContext.WalletTransactions.Add(new WalletTransaction
        {
            TransactionID = Guid.NewGuid(),
            UserID = fromUserId,
            Type = WalletTxType.TransferOut,
            Status = WalletTxStatus.Completed,
            Amount = amount,
            Description = fromDescription,
            CreatedAt = now
        });
        _dbContext.WalletTransactions.Add(new WalletTransaction
        {
            TransactionID = Guid.NewGuid(),
            UserID = toUserId,
            Type = WalletTxType.TransferIn,
            Status = WalletTxStatus.Completed,
            Amount = amount,
            Description = toDescription,
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await dbTx.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<(List<AdminTransactionResponse> Items, int Total)> GetAllTransactionsAsync(string? type, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var q = _dbContext.WalletTransactions.AsNoTracking().AsQueryable();

        var typeFilter = ParseType(type);
        if (typeFilter is not null)
            q = q.Where(x => x.Type == typeFilter);

        var statusFilter = ParseStatus(status);
        if (statusFilter is not null)
            q = q.Where(x => x.Status == statusFilter);

        var total = await q.CountAsync(cancellationToken);

        var rows = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.TransactionID,
                x.UserID,
                UserName = x.Wallet.User.Fullname,
                UserEmail = x.Wallet.User.Email,
                UserRole = x.Wallet.User.Role,
                TutorType = x.Wallet.User.Tutor != null ? (TutorType?)x.Wallet.User.Tutor.TutorType : null,
                x.Type,
                x.Amount,
                x.Status,
                x.Description,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(x => new AdminTransactionResponse
        {
            Id = x.TransactionID,
            UserId = x.UserID,
            User = x.UserName ?? string.Empty,
            Email = x.UserEmail ?? string.Empty,
            UserRole = RoleToWire(x.UserRole, x.TutorType),
            Type = TypeToWire(x.Type),
            Amount = x.Amount,
            Status = x.Status.ToString().ToLowerInvariant(),
            Description = x.Description,
            Date = x.CreatedAt
        }).ToList();

        return (items, total);
    }

    private static WalletTxType? ParseType(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "deposit" => WalletTxType.Deposit,
            "escrow_in" or "tuition_payment" => WalletTxType.EscrowIn,
            "escrow_release" => WalletTxType.EscrowRelease,
            "withdrawal" => WalletTxType.Withdrawal,
            "refund" => WalletTxType.Refund,
            "platform_fee" => WalletTxType.PlatformFee,
            "transfer_in" => WalletTxType.TransferIn,
            "transfer_out" => WalletTxType.TransferOut,
            _ => null
        };

    private static WalletTxStatus? ParseStatus(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "pending" => WalletTxStatus.Pending,
            "completed" => WalletTxStatus.Completed,
            "failed" => WalletTxStatus.Failed,
            _ => null
        };

    private static string TypeToWire(WalletTxType type) => type switch
    {
        WalletTxType.Deposit => "deposit",
        WalletTxType.EscrowIn => "escrow_in",
        WalletTxType.EscrowRelease => "escrow_release",
        WalletTxType.Withdrawal => "withdrawal",
        WalletTxType.Refund => "refund",
        WalletTxType.PlatformFee => "platform_fee",
        WalletTxType.TransferIn => "transfer_in",
        WalletTxType.TransferOut => "transfer_out",
        _ => type.ToString().ToLowerInvariant()
    };

    private static string RoleToWire(UserRole role, TutorType? tutorType) => role switch
    {
        UserRole.Admin => "admin",
        UserRole.Parent => "parent",
        UserRole.Student => "student",
        UserRole.Tutor => tutorType == TutorType.Teacher ? "teacher" : "tutor",
        _ => role.ToString().ToLowerInvariant()
    };
}

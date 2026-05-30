using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Withdrawals;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class WithdrawalRepository(ApplicationDbContext dbContext) : IWithdrawalRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<WithdrawalResponse> CreateAsync(Guid tutorId, CreateWithdrawalRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var method = request.Method.Trim().ToLowerInvariant();

        // One transaction: read wallet → check balance → debit → insert row → commit.
        // Prevents double-spend if a tutor races two withdraw calls.
        await using var dbTx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserID == tutorId, cancellationToken)
            ?? throw new BadRequestException("Wallet not found for this tutor.");

        if (wallet.Balance < request.Amount)
            throw new BadRequestException(
                $"Insufficient balance. Requested: {request.Amount:N0} VND, available: {wallet.Balance:N0} VND.");

        // Lock funds by debiting now; the reject endpoint will refund this on rejection.
        wallet.Balance -= request.Amount;
        wallet.UpdatedAt = now;

        var withdrawal = new Withdrawal
        {
            WithdrawalID = Guid.NewGuid(),
            TutorID = tutorId,
            Amount = request.Amount,
            Method = method,
            BankAccount = request.BankAccount,
            BankName = request.BankName,
            Note = request.Note,
            Status = WithdrawalStatus.Pending,
            RequestedAt = now
        };
        _dbContext.Withdrawals.Add(withdrawal);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await dbTx.CommitAsync(cancellationToken);

        return new WithdrawalResponse
        {
            Id = withdrawal.WithdrawalID,
            Amount = withdrawal.Amount,
            Method = withdrawal.Method,
            BankName = withdrawal.BankName ?? string.Empty,
            BankAccount = withdrawal.BankAccount ?? string.Empty,
            Note = withdrawal.Note,
            Status = "pending",
            RequestDate = withdrawal.RequestedAt
        };
    }
}

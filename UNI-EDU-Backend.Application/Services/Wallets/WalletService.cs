using UNI_EDU_Backend.Application.DTOs.Wallets;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Wallets;

public class WalletService(IWalletRepository walletRepo) : IWalletService
{
    private readonly IWalletRepository _walletRepo = walletRepo;

    public async Task<WalletResponse> GetMyWalletAsync(Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var balance = await _walletRepo.GetBalanceAsync(callerUserId, cancellationToken);

        // escrowBalance is tutor-only (TutorType.Teacher also carries UserRole.Tutor). It is the
        // tutor's pending earnings, computed from the Classes table — NOT read from
        // Wallet.EscrowBalance (that column tracks the student/payer side and is 0 for a tutor).
        decimal? escrowBalance = (callerRole ?? string.Empty).Trim() == "Tutor"
            ? await _walletRepo.GetTutorOutstandingEscrowAsync(callerUserId, cancellationToken)
            : null;

        return new WalletResponse
        {
            Balance = balance,
            EscrowBalance = escrowBalance
        };
    }
}

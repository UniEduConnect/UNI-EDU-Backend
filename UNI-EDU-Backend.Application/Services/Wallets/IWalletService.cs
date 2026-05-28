using UNI_EDU_Backend.Application.DTOs.Wallets;

namespace UNI_EDU_Backend.Application.Services.Wallets;

public interface IWalletService
{
    Task<WalletResponse> GetMyWalletAsync(Guid callerUserId, string callerRole, CancellationToken cancellationToken);
}

using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Wallets;

namespace UNI_EDU_Backend.Application.Services.Wallets;

public interface IWalletService
{
    Task<WalletResponse> GetMyWalletAsync(Guid callerUserId, string callerRole, CancellationToken cancellationToken);

    Task<PagedResult<TransactionResponse>> GetMyTransactionsAsync(TransactionListQuery query, Guid callerUserId, string callerRole, CancellationToken cancellationToken);

    // Creates a Pending deposit and a provider payment order; returns the pay URL. No balance change.
    Task<DepositResponse> InitiateDepositAsync(DepositRequest request, Guid callerUserId, CancellationToken cancellationToken);

    // Verifies + applies a Momo IPN callback (idempotent). Throws on bad signature.
    Task<DepositSettleOutcome> HandleMomoIpnAsync(MomoIpnCallback callback, CancellationToken cancellationToken);
}

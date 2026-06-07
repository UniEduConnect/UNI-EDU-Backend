using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Wallets;
using UNI_EDU_Backend.Application.DTOs.Withdrawals;

namespace UNI_EDU_Backend.Application.Services.Wallets;

public interface IWalletService
{
    Task<WalletResponse> GetMyWalletAsync(Guid callerUserId, string callerRole, CancellationToken cancellationToken);

    Task<PagedResult<TransactionResponse>> GetMyTransactionsAsync(TransactionListQuery query, Guid callerUserId, string callerRole, CancellationToken cancellationToken);

    // Creates a Pending deposit and a provider payment order; returns the pay URL. No balance change.
    Task<DepositResponse> InitiateDepositAsync(DepositRequest request, Guid callerUserId, CancellationToken cancellationToken);

    // Verifies + applies a Momo IPN callback (idempotent). Throws on bad signature.
    Task<DepositSettleOutcome> HandleMomoIpnAsync(MomoIpnCallback callback, CancellationToken cancellationToken);

    // Verifies + applies a VNPay IPN (idempotent). Returns the VNPay-formatted ack response —
    // never throws on bad signature so VNPay receives a structured "97 Invalid Checksum".
    Task<VnPayIpnResponse> HandleVnPayIpnAsync(IReadOnlyDictionary<string, string> vnpFields, string providedHash, CancellationToken cancellationToken);

    // Tutors only: creates a Pending withdrawal request and locks the requested amount by
    // debiting wallet.Balance. Finance approves/rejects via the (future) P3 endpoints.
    Task<WithdrawalResponse> CreateWithdrawalAsync(CreateWithdrawalRequest request, Guid callerUserId, CancellationToken cancellationToken);

    // Mock-deposit flow used while Momo/VNPay sandbox config is unavailable. NO gateway call,
    // NO signature, NO IPN — purely DB-side bookkeeping for the dev/demo path.
    //   - InitiateTestDepositAsync: creates a Pending WalletTransaction with Method = "test".
    //     Returns the same DepositResponse shape as the real flow (PayUrl is empty).
    //   - ConfirmTestDepositAsync: settles that pending row (only if owned by caller, still
    //     pending, and Method = "test") and credits the wallet. Replaces the IPN step that
    //     would otherwise come from Momo/VNPay.
    Task<DepositResponse> InitiateTestDepositAsync(DepositTestRequest request, Guid callerUserId, CancellationToken cancellationToken);
    Task<TestDepositConfirmResponse> ConfirmTestDepositAsync(Guid transactionId, Guid callerUserId, CancellationToken cancellationToken);
}

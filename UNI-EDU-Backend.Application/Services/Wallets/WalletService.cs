using System.Text.Json;
using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Wallets;
using UNI_EDU_Backend.Application.DTOs.Withdrawals;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Services.Wallets;

public class WalletService(
    IWalletRepository walletRepo,
    IWithdrawalRepository withdrawalRepo,
    IValidator<TransactionListQuery> transactionsValidator,
    IValidator<DepositRequest> depositValidator,
    IValidator<CreateWithdrawalRequest> withdrawalValidator,
    IValidator<DepositTestRequest> depositTestValidator,
    IEnumerable<IPaymentGateway> paymentGateways,
    IMomoGateway momoGateway,
    IVnPayGateway vnPayGateway,
    IPayOsGateway payOsGateway) : IWalletService
{
    private const int PageSize = 10;

    // Used as the WalletTransaction.Method value for mock deposits. Real deposits use
    // "momo" / "vnpay" / "bank"; the confirm endpoint refuses to settle anything except "test".
    private const string TestMethod = "test";

    private readonly IWalletRepository _walletRepo = walletRepo;
    private readonly IWithdrawalRepository _withdrawalRepo = withdrawalRepo;
    private readonly IValidator<TransactionListQuery> _transactionsValidator = transactionsValidator;
    private readonly IValidator<DepositRequest> _depositValidator = depositValidator;
    private readonly IValidator<CreateWithdrawalRequest> _withdrawalValidator = withdrawalValidator;
    private readonly IValidator<DepositTestRequest> _depositTestValidator = depositTestValidator;
    private readonly IEnumerable<IPaymentGateway> _paymentGateways = paymentGateways;
    private readonly IMomoGateway _momoGateway = momoGateway;
    private readonly IVnPayGateway _vnPayGateway = vnPayGateway;
    private readonly IPayOsGateway _payOsGateway = payOsGateway;

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

    public async Task<PagedResult<TransactionResponse>> GetMyTransactionsAsync(TransactionListQuery query, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        await _transactionsValidator.EnsureValidAsync(query, cancellationToken);

        var role = (callerRole ?? string.Empty).Trim();

        var (rows, total) = await _walletRepo.GetTransactionsAsync(callerUserId, query.Page, PageSize, cancellationToken);

        var items = rows.Select(r =>
        {
            var resp = new TransactionResponse
            {
                Id = r.TransactionId,
                Type = MapType(r.Type, role),
                Amount = r.Amount,
                Description = r.Description,
                Date = r.CreatedAt.ToString("yyyy-MM-dd"),
                Status = "completed"
            };

            // Superset: each role carries only its own related-id field.
            switch (role)
            {
                case "Tutor": resp.ClassId = r.RelatedClassId; break;
                case "Student": resp.RelatedId = r.RelatedClassId; break;
                case "Parent": resp.ChildId = r.ChildId; break;
            }

            return resp;
        }).ToList();

        return new PagedResult<TransactionResponse>
        {
            Items = items,
            Total = total,
            Page = query.Page,
            PageSize = PageSize
        };
    }

    public async Task<DepositResponse> InitiateDepositAsync(DepositRequest request, Guid callerUserId, CancellationToken cancellationToken)
    {
        await _depositValidator.EnsureValidAsync(request, cancellationToken);

        var method = request.Method.Trim().ToLowerInvariant();
        var gateway = _paymentGateways.FirstOrDefault(g => g.Method == method)
            ?? throw new BadRequestException($"Payment method '{method}' is not available yet.");

        // VND is a whole-unit currency — pin the amount so the pending row matches what the
        // provider confirms in the IPN (avoids spurious amount-mismatch failures).
        var amount = Math.Truncate(request.Amount);
        // PayOS requires a numeric orderCode; we store it as the transaction's ProviderRef
        // (CreatePendingDepositAsync sets ProviderRef = orderId), so the webhook settles by it.
        var orderId = method == "payos"
            ? ((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 100) + Random.Shared.Next(100)).ToString()
            : $"DEP-{Guid.NewGuid():N}";
        const string orderInfo = "Nap tien vao vi UNI-EDU";

        // Create the Pending ledger row BEFORE calling the provider, so the IPN always has a
        // record to settle. Balance stays untouched until the IPN confirms.
        var transactionId = await _walletRepo.CreatePendingDepositAsync(
            callerUserId, amount, method, orderId, orderInfo, cancellationToken);

        var creation = await gateway.CreatePaymentAsync(
            new PaymentCreationCommand(amount, orderId, orderInfo), cancellationToken);

        return new DepositResponse
        {
            TransactionId = transactionId,
            PayUrl = creation.PayUrl,
            Status = "pending"
        };
    }

    public async Task<DepositSettleOutcome> HandleMomoIpnAsync(MomoIpnCallback callback, CancellationToken cancellationToken)
    {
        // Signature is the ONLY trust anchor — the caller is unauthenticated.
        if (!_momoGateway.VerifyIpnSignature(callback))
            throw new Exceptions.UnauthorizedAccessException("Invalid Momo IPN signature.");

        var success = callback.ResultCode == 0;

        // Idempotent credit — trust Momo's confirmed amount, not anything client-supplied.
        return await _walletRepo.SettleDepositAsync(
            callback.OrderId, success, callback.TransId.ToString(), callback.Amount, cancellationToken);
    }

    public async Task<VnPayIpnResponse> HandleVnPayIpnAsync(IReadOnlyDictionary<string, string> vnpFields, string providedHash, CancellationToken cancellationToken)
    {
        // Signature is the only trust anchor — fail first, before touching the DB.
        if (!_vnPayGateway.VerifyCallbackSignature(vnpFields, providedHash))
            return new VnPayIpnResponse { RspCode = "97", Message = "Invalid Checksum" };

        if (!vnpFields.TryGetValue("vnp_TxnRef", out var orderId) ||
            !vnpFields.TryGetValue("vnp_Amount", out var amountStr) ||
            !vnpFields.TryGetValue("vnp_ResponseCode", out var responseCode) ||
            !vnpFields.TryGetValue("vnp_TransactionStatus", out var transactionStatus) ||
            !long.TryParse(amountStr, out var amountSubunits))
        {
            return new VnPayIpnResponse { RspCode = "99", Message = "Unknown error" };
        }

        var transactionNo = vnpFields.TryGetValue("vnp_TransactionNo", out var tn) ? tn : string.Empty;
        var success = responseCode == "00" && transactionStatus == "00";
        // VNPay amount is in subunits (×100). Convert back to whole VND to compare to the stored row.
        var confirmedAmount = amountSubunits / 100m;

        var outcome = await _walletRepo.SettleDepositAsync(orderId, success, transactionNo, confirmedAmount, cancellationToken);

        return outcome switch
        {
            DepositSettleOutcome.NotFound       => new VnPayIpnResponse { RspCode = "01", Message = "Order Not Found" },
            DepositSettleOutcome.AlreadySettled => new VnPayIpnResponse { RspCode = "02", Message = "Order already confirmed" },
            DepositSettleOutcome.AmountMismatch => new VnPayIpnResponse { RspCode = "04", Message = "Invalid Amount" },
            // Provider reported failure but we recorded it — ack with 00 so VNPay stops retrying.
            DepositSettleOutcome.Failed         => new VnPayIpnResponse { RspCode = "00", Message = "Confirm Success" },
            DepositSettleOutcome.Credited       => new VnPayIpnResponse { RspCode = "00", Message = "Confirm Success" },
            _                                   => new VnPayIpnResponse { RspCode = "99", Message = "Unknown error" }
        };
    }

    // Confirms a deposit from the signed VNPay browser-return params. Same trust anchor + idempotent
    // settle as the IPN, so the wallet credits even if the server-to-server IPN never arrives.
    public async Task<VnPayReturnResult> HandleVnPayReturnAsync(IReadOnlyDictionary<string, string> vnpFields, string providedHash, CancellationToken cancellationToken)
    {
        if (!_vnPayGateway.VerifyCallbackSignature(vnpFields, providedHash))
            return new VnPayReturnResult { Status = "invalid-signature" };

        if (!vnpFields.TryGetValue("vnp_TxnRef", out var orderId) ||
            !vnpFields.TryGetValue("vnp_Amount", out var amountStr) ||
            !vnpFields.TryGetValue("vnp_ResponseCode", out var responseCode) ||
            !long.TryParse(amountStr, out var amountSubunits))
        {
            return new VnPayReturnResult { Status = "invalid" };
        }

        var success = responseCode == "00";
        var transactionNo = vnpFields.TryGetValue("vnp_TransactionNo", out var tn) ? tn : string.Empty;
        var confirmedAmount = amountSubunits / 100m;

        var outcome = await _walletRepo.SettleDepositAsync(orderId, success, transactionNo, confirmedAmount, cancellationToken);

        return new VnPayReturnResult
        {
            Amount = confirmedAmount,
            Status = outcome switch
            {
                DepositSettleOutcome.Credited => "credited",
                DepositSettleOutcome.AlreadySettled => "already",
                DepositSettleOutcome.AmountMismatch => "amount-mismatch",
                DepositSettleOutcome.NotFound => "not-found",
                DepositSettleOutcome.Failed => "failed",
                _ => "invalid",
            },
        };
    }

    public async Task<DepositSettleOutcome> HandlePayOsWebhookAsync(PayOsWebhookPayload payload, CancellationToken cancellationToken)
    {
        // Signature is the only trust anchor — the caller is unauthenticated.
        if (!_payOsGateway.VerifyWebhookSignature(payload))
            throw new Exceptions.UnauthorizedAccessException("Invalid PayOS webhook signature.");

        if (payload.Data.ValueKind != JsonValueKind.Object || !payload.Data.TryGetProperty("orderCode", out var ocEl))
            return DepositSettleOutcome.NotFound;

        var orderCode = ocEl.GetInt64().ToString(); // == the stored ProviderRef
        var amount = payload.Data.TryGetProperty("amount", out var amEl) ? amEl.GetInt64() : 0L;
        var providerTxn = payload.Data.TryGetProperty("reference", out var rfEl) ? (rfEl.GetString() ?? string.Empty) : string.Empty;
        // PayOS confirms success via top-level success / code "00".
        var success = payload.Success || string.Equals(payload.Code, "00", StringComparison.Ordinal);

        return await _walletRepo.SettleDepositAsync(orderCode, success, providerTxn, amount, cancellationToken);
    }

    public async Task<WithdrawalResponse> CreateWithdrawalAsync(CreateWithdrawalRequest request, Guid callerUserId, CancellationToken cancellationToken)
    {
        // Tutor-only is enforced at the controller via [Authorize(Roles = "Tutor")].
        await _withdrawalValidator.EnsureValidAsync(request, cancellationToken);

        // Balance check + debit + insert happen atomically inside the repo (single DB transaction)
        // to prevent double-spend on concurrent requests.
        return await _withdrawalRepo.CreateAsync(callerUserId, request, cancellationToken);
    }

    public async Task<DepositResponse> InitiateTestDepositAsync(DepositTestRequest request, Guid callerUserId, CancellationToken cancellationToken)
    {
        await _depositTestValidator.EnsureValidAsync(request, cancellationToken);

        var amount = Math.Truncate(request.Amount);
        var orderId = $"DEP-TEST-{Guid.NewGuid():N}";
        const string orderInfo = "Nap tien vao vi UNI-EDU (TEST)";

        var transactionId = await _walletRepo.CreatePendingDepositAsync(
            callerUserId, amount, TestMethod, orderId, orderInfo, cancellationToken);

        return new DepositResponse
        {
            TransactionId = transactionId,
            // No gateway, no redirect. UI calls /deposit-test/{transactionId}/confirm directly.
            PayUrl = string.Empty,
            Status = "pending"
        };
    }

    public async Task<TestDepositConfirmResponse> ConfirmTestDepositAsync(Guid transactionId, Guid callerUserId, CancellationToken cancellationToken)
    {
        var lookup = await _walletRepo.LookupTestDepositAsync(transactionId, cancellationToken)
            ?? throw new NotFoundException($"Deposit transaction '{transactionId}' not found.");

        if (lookup.UserId != callerUserId)
            throw new ForbiddenAccessException("You can only confirm your own test deposits.");

        // Hard wall against this endpoint touching real Momo/VNPay pending rows. Those must
        // settle through their real IPN with a verified signature.
        if (!string.Equals(lookup.Method, TestMethod, StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("This endpoint can only confirm test deposits.");

        if (lookup.Status != WalletTxStatus.Pending)
            throw new BadRequestException($"Deposit is no longer pending (current status: {lookup.Status}).");

        var outcome = await _walletRepo.SettleDepositAsync(
            lookup.OrderId, success: true, providerTxnId: $"MOCK-{Guid.NewGuid():N}", confirmedAmount: lookup.Amount, cancellationToken);

        if (outcome != DepositSettleOutcome.Credited)
            // Could happen if a concurrent confirm raced us through SettleDepositAsync. Surface
            // as 400 so the UI can re-fetch the wallet and show the current state.
            throw new BadRequestException($"Could not credit deposit (outcome: {outcome}).");

        var balance = await _walletRepo.GetBalanceAsync(callerUserId, cancellationToken);

        return new TestDepositConfirmResponse
        {
            TransactionId = transactionId,
            Status = "completed",
            Balance = balance
        };
    }

    // Student/parent call the booking debit "tuition_payment"; the ledger stores it as EscrowIn.
    // Tutors see the faithful enum name. escrow_release/platform_fee don't surface on
    // student/parent feeds in practice (those hit the tutor side).
    private static string MapType(WalletTxType type, string role)
    {
        if ((role == "Student" || role == "Parent") && type == WalletTxType.EscrowIn)
            return "tuition_payment";

        return type switch
        {
            WalletTxType.Deposit => "deposit",
            WalletTxType.EscrowIn => "escrow_in",
            WalletTxType.EscrowRelease => "escrow_release",
            WalletTxType.Withdrawal => "withdrawal",
            WalletTxType.Refund => "refund",
            WalletTxType.PlatformFee => "platform_fee",
            _ => type.ToString().ToLowerInvariant()
        };
    }
}

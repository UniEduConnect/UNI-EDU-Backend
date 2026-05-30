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
    IEnumerable<IPaymentGateway> paymentGateways,
    IMomoGateway momoGateway,
    IVnPayGateway vnPayGateway) : IWalletService
{
    private const int PageSize = 10;

    private readonly IWalletRepository _walletRepo = walletRepo;
    private readonly IWithdrawalRepository _withdrawalRepo = withdrawalRepo;
    private readonly IValidator<TransactionListQuery> _transactionsValidator = transactionsValidator;
    private readonly IValidator<DepositRequest> _depositValidator = depositValidator;
    private readonly IValidator<CreateWithdrawalRequest> _withdrawalValidator = withdrawalValidator;
    private readonly IEnumerable<IPaymentGateway> _paymentGateways = paymentGateways;
    private readonly IMomoGateway _momoGateway = momoGateway;
    private readonly IVnPayGateway _vnPayGateway = vnPayGateway;

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
        var orderId = $"DEP-{Guid.NewGuid():N}";
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

    public async Task<WithdrawalResponse> CreateWithdrawalAsync(CreateWithdrawalRequest request, Guid callerUserId, CancellationToken cancellationToken)
    {
        // Tutor-only is enforced at the controller via [Authorize(Roles = "Tutor")].
        await _withdrawalValidator.EnsureValidAsync(request, cancellationToken);

        // Balance check + debit + insert happen atomically inside the repo (single DB transaction)
        // to prevent double-spend on concurrent requests.
        return await _withdrawalRepo.CreateAsync(callerUserId, request, cancellationToken);
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

using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Wallets;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Services.Wallets;

public class WalletService(
    IWalletRepository walletRepo,
    IValidator<TransactionListQuery> transactionsValidator) : IWalletService
{
    private const int PageSize = 10;

    private readonly IWalletRepository _walletRepo = walletRepo;
    private readonly IValidator<TransactionListQuery> _transactionsValidator = transactionsValidator;

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

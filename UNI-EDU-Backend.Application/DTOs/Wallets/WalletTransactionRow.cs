using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.DTOs.Wallets;

// Repository → service transport for a single transaction row. The enum is kept raw here;
// the service maps it to a role-specific wire string.
public record WalletTransactionRow(
    Guid TransactionId,
    WalletTxType Type,
    decimal Amount,
    string Description,
    DateTime CreatedAt,
    Guid? RelatedClassId,
    Guid? ChildId);

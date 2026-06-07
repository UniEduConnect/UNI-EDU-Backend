using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.DTOs.Wallets;

// Repo -> service transport for the mock-confirm flow. The service does the ownership +
// status + method-is-test checks before calling SettleDepositAsync(orderId, ...).
public record TestDepositLookup(
    Guid UserId,
    string OrderId,
    string? Method,
    WalletTxStatus Status,
    decimal Amount);

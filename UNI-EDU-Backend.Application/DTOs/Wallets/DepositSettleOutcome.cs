namespace UNI_EDU_Backend.Application.DTOs.Wallets;

// Result of applying a provider IPN to a pending deposit. Drives the IPN response + logging.
public enum DepositSettleOutcome
{
    NotFound,        // no pending deposit for that order id
    AlreadySettled,  // already Completed/Failed — idempotent no-op (webhook retry)
    AmountMismatch,  // provider amount != pending amount — marked Failed, not credited
    Failed,          // provider reported failure — marked Failed
    Credited         // success: marked Completed and balance credited
}

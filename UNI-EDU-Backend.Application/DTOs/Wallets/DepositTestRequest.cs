namespace UNI_EDU_Backend.Application.DTOs.Wallets;

// Body for POST /api/wallet/deposit-test. No Method field — the test flow always stores
// Method = "test" so it can never be settled by a real Momo/VNPay IPN by accident.
public class DepositTestRequest
{
    public decimal Amount { get; set; }

    // Optional bank-transfer memo (e.g. "UNIEDU NAP A1B2C3") — appended to the transaction
    // description so it shows as the transaction content in history/detail views.
    public string? Note { get; set; }
}

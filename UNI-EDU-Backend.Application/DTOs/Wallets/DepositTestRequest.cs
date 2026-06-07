namespace UNI_EDU_Backend.Application.DTOs.Wallets;

// Body for POST /api/wallet/deposit-test. No Method field — the test flow always stores
// Method = "test" so it can never be settled by a real Momo/VNPay IPN by accident.
public class DepositTestRequest
{
    public decimal Amount { get; set; }
}

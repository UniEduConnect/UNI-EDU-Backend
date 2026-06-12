using System.Text.Json.Serialization;

namespace UNI_EDU_Backend.Application.DTOs.Wallets;

// VNPay expects the IPN response in this exact PascalCase shape:
//   { "RspCode": "00", "Message": "Confirm Success" }
// Standard RspCodes: 00 ok, 01 not found, 02 already confirmed, 04 invalid amount,
// 97 invalid signature, 99 unknown error.
public class VnPayIpnResponse
{
    [JsonPropertyName("RspCode")]
    public string RspCode { get; set; } = string.Empty;

    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;
}

// Result of confirming a deposit from the VNPay browser-return params (signed). Lets the wallet
// credit even when the server-to-server IPN doesn't arrive (e.g. ngrok in dev). Idempotent.
public class VnPayReturnResult
{
    public string Status { get; set; } = string.Empty; // credited | already | failed | amount-mismatch | not-found | invalid-signature | invalid
    public decimal Amount { get; set; }
}

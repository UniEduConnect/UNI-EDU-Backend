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

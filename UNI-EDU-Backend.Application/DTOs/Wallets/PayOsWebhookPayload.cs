using System.Text.Json;

namespace UNI_EDU_Backend.Application.DTOs.Wallets;

// PayOS webhook body. `Data` is kept as a raw JsonElement so the signature can be
// recomputed over its sorted key=value form (PayOS's verification algorithm).
public class PayOsWebhookPayload
{
    public string? Code { get; set; }
    public string? Desc { get; set; }
    public bool Success { get; set; }
    public JsonElement Data { get; set; }
    public string? Signature { get; set; }
}

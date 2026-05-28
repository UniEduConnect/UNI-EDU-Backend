using System.Text.Json.Serialization;

namespace UNI_EDU_Backend.Application.DTOs.Wallets;

public class WalletResponse
{
    public decimal Balance { get; set; }

    // Tutors only: pending earnings computed across their classes (Σ EscrowAmount - EscrowReleased).
    // Omitted from the payload for other roles.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? EscrowBalance { get; set; }
}

using System.Text.Json.Serialization;

namespace UNI_EDU_Backend.Application.DTOs.Wallets;

// Superset across the tutor/student/parent transaction shapes. Role-irrelevant fields are
// left null and dropped from the payload (WhenWritingNull).
public class TransactionResponse
{
    public Guid Id { get; set; }

    // snake_case, role-mapped (EscrowIn → tuition_payment for student/parent).
    public string Type { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;

    // No status column on WalletTransaction yet — every stored row is settled.
    public string Status { get; set; } = "completed";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ClassId { get; set; }    // tutor view

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? RelatedId { get; set; }  // student view, may be examId or classId

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ChildId { get; set; }    // parent view (related class's student)

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PaymentMethod { get; set; }  // not modeled yet — always null
}

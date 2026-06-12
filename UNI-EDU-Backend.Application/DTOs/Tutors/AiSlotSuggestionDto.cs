namespace UNI_EDU_Backend.Application.DTOs.Tutors;

// An AI-ranked weekly study slot suggestion (tutor ∩ student free time).
public class AiSlotSuggestionDto
{
    public string Day { get; set; } = string.Empty;   // "Thứ 2".."Chủ nhật"
    public string Time { get; set; } = string.Empty;  // "19:00-21:00"
    public string Reason { get; set; } = string.Empty;
    public int Score { get; set; }                     // 0–100
}

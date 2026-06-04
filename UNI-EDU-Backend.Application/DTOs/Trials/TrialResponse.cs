namespace UNI_EDU_Backend.Application.DTOs.Trials;

public class TrialResponse
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid TutorId { get; set; }
    public string TutorName { get; set; } = string.Empty;

    public Guid? SubjectId { get; set; }
    public string? Subject { get; set; }

    public string Day { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string? Message { get; set; }

    // "pending" | "accepted" | "declined"
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

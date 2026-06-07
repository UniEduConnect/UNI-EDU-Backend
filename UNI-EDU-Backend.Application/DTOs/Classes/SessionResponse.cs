namespace UNI_EDU_Backend.Application.DTOs.Classes;

public class SessionResponse
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;

    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public string? Content { get; set; }
    public string? Notes { get; set; }
    public string? Homework { get; set; }
    public List<string> HomeworkFiles { get; set; } = new();

    public int? Rating { get; set; }
    public string? RatingComment { get; set; }

    public string? AbsenceReason { get; set; }
    public string? AbsenceRequestedBy { get; set; }
    public bool? AbsenceApproved { get; set; }
}

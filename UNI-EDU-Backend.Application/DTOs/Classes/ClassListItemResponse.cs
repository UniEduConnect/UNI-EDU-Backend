namespace UNI_EDU_Backend.Application.DTOs.Classes;

public class ClassListItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public Guid TutorId { get; set; }
    public Guid StudentId { get; set; }
    public Guid SubjectId { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string TutorName { get; set; } = string.Empty;
    public string TutorAvatar { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public decimal Fee { get; set; }

    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<WeeklySlotDto> WeeklySlots { get; set; } = [];
}

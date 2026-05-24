namespace UNI_EDU_Backend.Application.DTOs.Classes;

public class ClassResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid TutorId { get; set; }
    public Guid StudentId { get; set; }
    public Guid SubjectId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public decimal Fee { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<WeeklySlotDto> WeeklySlots { get; set; } = new();
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public decimal EscrowAmount { get; set; }
    public decimal EscrowReleased { get; set; }
    public string EscrowStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime StartDate { get; set; }
    public List<SessionResponse> Sessions { get; set; } = new();
}

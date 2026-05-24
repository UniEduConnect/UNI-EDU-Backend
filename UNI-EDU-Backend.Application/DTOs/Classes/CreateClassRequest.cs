namespace UNI_EDU_Backend.Application.DTOs.Classes;

public class CreateClassRequest
{
    public Guid StudentId { get; set; } = new Guid("44444444-4444-4444-4444-000000000001");
    public Guid TutorId { get; set; } = new Guid("22222222-2222-2222-2222-000000000001");
    public Guid SubjectId { get; set; } = new Guid("11111111-1111-1111-1111-000000000001");
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public int TotalSessions { get; set; }
    public List<WeeklySlotDto> WeeklySlots { get; set; } = new();
    public string Format { get; set; } = "online";
    public decimal Fee { get; set; }
}

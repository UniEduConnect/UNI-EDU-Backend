namespace UNI_EDU_Backend.Application.DTOs.Classes;

public class SessionResponse
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
}

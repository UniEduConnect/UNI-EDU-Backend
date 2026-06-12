namespace UNI_EDU_Backend.Application.DTOs.Chat;

public class MessageResponse
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid SenderId { get; set; }

    // "student" | "tutor" | "parent"
    public string Sender { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Read { get; set; }
}

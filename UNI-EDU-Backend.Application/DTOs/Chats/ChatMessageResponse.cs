namespace UNI_EDU_Backend.Application.DTOs.Chats;

public class ChatMessageResponse
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    /// <summary>"admin" | "tutor" | "student" | "parent"</summary>
    public string SenderRole { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    /// <summary>True when the authenticated caller authored this message.</summary>
    public bool IsMine { get; set; }
}

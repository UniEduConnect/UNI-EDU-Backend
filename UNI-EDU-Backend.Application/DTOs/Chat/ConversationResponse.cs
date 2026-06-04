namespace UNI_EDU_Backend.Application.DTOs.Chat;

// Roll-up row for the chat sidebar (one per class the caller participates in that has messages).
public class ConversationResponse
{
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string OtherPartyName { get; set; } = string.Empty;
    public string? OtherPartyAvatar { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastTimestamp { get; set; }
    public int UnreadCount { get; set; }
}

namespace UNI_EDU_Backend.Application.DTOs.Chat;

// Roll-up row for the chat sidebar (one per class the caller participates in; LastMessage/LastTimestamp are null until the first message).
public class ConversationResponse
{
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string OtherPartyName { get; set; } = string.Empty;
    public string? OtherPartyAvatar { get; set; }
    // Class participants, surfaced so the chat list can search/display both the student and the parent.
    public string StudentName { get; set; } = string.Empty;
    public string? ParentName { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastTimestamp { get; set; }
    public int UnreadCount { get; set; }
}

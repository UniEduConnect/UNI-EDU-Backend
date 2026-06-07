namespace UNI_EDU_Backend.Application.DTOs.Chats;

public class DmMessageResponse
{
    public Guid Id { get; set; }
    /// <summary>The other party in the conversation from the caller's perspective.</summary>
    public Guid ContactId { get; set; }
    public Guid SenderId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    /// <summary>True when the authenticated caller authored this message.</summary>
    public bool IsMine { get; set; }
}

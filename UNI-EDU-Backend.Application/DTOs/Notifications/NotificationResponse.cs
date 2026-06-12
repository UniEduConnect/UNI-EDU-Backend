namespace UNI_EDU_Backend.Application.DTOs.Notifications;

public class NotificationResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    // "info" | "warning" | "success" | "error"
    public string Type { get; set; } = "info";

    public string? Link { get; set; }
    public bool Read { get; set; }
    public DateTime CreatedAt { get; set; }
}

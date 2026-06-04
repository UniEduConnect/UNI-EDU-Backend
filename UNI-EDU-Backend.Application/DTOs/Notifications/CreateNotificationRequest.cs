namespace UNI_EDU_Backend.Application.DTOs.Notifications;

// Admin-initiated notification to a specific user.
public class CreateNotificationRequest
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    // "info" | "warning" | "success" | "error"
    public string Type { get; set; } = "info";
    public string? Link { get; set; }
}

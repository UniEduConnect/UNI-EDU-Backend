namespace UNI_EDU_Backend.Application.DTOs.Notifications;

public class NotificationListQuery
{
    // When true, only unread notifications are returned.
    public bool UnreadOnly { get; set; }

    // 1-based. Default page size is 20 (see NotificationService.PageSize).
    public int Page { get; set; } = 1;
}

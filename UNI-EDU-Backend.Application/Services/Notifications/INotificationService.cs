using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Notifications;

namespace UNI_EDU_Backend.Application.Services.Notifications;

public interface INotificationService
{
    Task<PagedResult<NotificationResponse>> GetMineAsync(NotificationListQuery query, Guid userId, CancellationToken cancellationToken);
    Task<int> UnreadCountAsync(Guid userId, CancellationToken cancellationToken);
    Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken);
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken);

    // Admin → send a notification to a specific user.
    Task<NotificationResponse> CreateForUserAsync(CreateNotificationRequest request, CancellationToken cancellationToken);
}

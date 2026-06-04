using UNI_EDU_Backend.Application.DTOs.Notifications;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface INotificationRepository
{
    Task<(List<NotificationResponse> Items, int Total)> GetByUserAsync(Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> UnreadCountAsync(Guid userId, CancellationToken cancellationToken);

    // False if no matching notification belongs to the user.
    Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken);
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken);
    Task<NotificationResponse> CreateAsync(Guid userId, string title, string message, string type, string? link, CancellationToken cancellationToken);
}

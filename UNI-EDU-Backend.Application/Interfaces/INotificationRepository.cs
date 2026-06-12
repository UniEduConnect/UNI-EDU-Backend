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

    // Notify the student of a session's class (and their linked parent, if any) about a class event.
    Task NotifyStudentSideAsync(Guid sessionId, string title, string message, string? link, CancellationToken cancellationToken);

    // Notify the tutor of a session's class about a class event.
    Task NotifyTutorAsync(Guid sessionId, string title, string message, string? link, CancellationToken cancellationToken);

    // Notify the student AND their parent with role-specific links (so each lands on the right page).
    Task NotifySessionPartiesAsync(Guid sessionId, string title, string message, string? studentLink, string? parentLink, CancellationToken cancellationToken);

    // Create one notification per recipient user id (used for ad-hoc fan-outs).
    Task CreateForManyAsync(IEnumerable<Guid> userIds, string title, string message, string type, string? link, CancellationToken cancellationToken);
}

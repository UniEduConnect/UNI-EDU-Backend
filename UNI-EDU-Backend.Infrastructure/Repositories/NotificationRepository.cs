using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Notifications;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class NotificationRepository(ApplicationDbContext dbContext) : INotificationRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<(List<NotificationResponse> Items, int Total)> GetByUserAsync(Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken)
    {
        var q = _dbContext.Notifications.AsNoTracking().Where(n => n.UserID == userId);
        if (unreadOnly)
            q = q.Where(n => !n.IsRead);

        var total = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationResponse
            {
                Id = n.NotificationID,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                Link = n.Link,
                Read = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<int> UnreadCountAsync(Guid userId, CancellationToken cancellationToken) =>
        _dbContext.Notifications.CountAsync(n => n.UserID == userId && !n.IsRead, cancellationToken);

    public async Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var n = await _dbContext.Notifications.FirstOrDefaultAsync(x => x.NotificationID == notificationId && x.UserID == userId, cancellationToken);
        if (n is null) return false;

        if (!n.IsRead)
        {
            n.IsRead = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        return true;
    }

    public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var unread = await _dbContext.Notifications.Where(n => n.UserID == userId && !n.IsRead).ToListAsync(cancellationToken);
        foreach (var n in unread) n.IsRead = true;
        if (unread.Count > 0) await _dbContext.SaveChangesAsync(cancellationToken);
        return unread.Count;
    }

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken) =>
        _dbContext.Users.AnyAsync(u => u.UserID == userId, cancellationToken);

    public async Task<NotificationResponse> CreateAsync(Guid userId, string title, string message, string type, string? link, CancellationToken cancellationToken)
    {
        var entity = new Notification
        {
            NotificationID = Guid.NewGuid(),
            UserID = userId,
            Title = title,
            Message = message,
            Type = type,
            Link = link,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Notifications.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new NotificationResponse
        {
            Id = entity.NotificationID,
            Title = entity.Title,
            Message = entity.Message,
            Type = entity.Type,
            Link = entity.Link,
            Read = entity.IsRead,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task NotifyStudentSideAsync(Guid sessionId, string title, string message, string? link, CancellationToken cancellationToken)
    {
        var info = await _dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.SessionID == sessionId)
            .Select(s => new { StudentUserId = s.Class.StudentID, ParentUserId = s.Class.Student.ParentID })
            .FirstOrDefaultAsync(cancellationToken);

        if (info is null) return;

        var recipients = new List<Guid> { info.StudentUserId };
        if (info.ParentUserId is Guid pid) recipients.Add(pid);

        await CreateForManyAsync(recipients, title, message, "info", link, cancellationToken);
    }

    public async Task NotifyTutorAsync(Guid sessionId, string title, string message, string? link, CancellationToken cancellationToken)
    {
        var tutorId = await _dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.SessionID == sessionId)
            .Select(s => (Guid?)s.Class.TutorID)
            .FirstOrDefaultAsync(cancellationToken);

        if (tutorId is Guid tid)
            await CreateForManyAsync(new[] { tid }, title, message, "warning", link, cancellationToken);
    }

    public async Task NotifySessionPartiesAsync(Guid sessionId, string title, string message, string? studentLink, string? parentLink, CancellationToken cancellationToken)
    {
        var info = await _dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.SessionID == sessionId)
            .Select(s => new { StudentUserId = s.Class.StudentID, ParentUserId = s.Class.Student.ParentID })
            .FirstOrDefaultAsync(cancellationToken);

        if (info is null) return;

        await CreateForManyAsync(new[] { info.StudentUserId }, title, message, "warning", studentLink, cancellationToken);
        if (info.ParentUserId is Guid pid)
            await CreateForManyAsync(new[] { pid }, title, message, "warning", parentLink, cancellationToken);
    }

    public async Task CreateForManyAsync(IEnumerable<Guid> userIds, string title, string message, string type, string? link, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var distinct = userIds.Distinct().ToList();
        if (distinct.Count == 0) return;

        foreach (var uid in distinct)
        {
            _dbContext.Notifications.Add(new Notification
            {
                NotificationID = Guid.NewGuid(),
                UserID = uid,
                Title = title,
                Message = message,
                Type = type,
                Link = link,
                IsRead = false,
                CreatedAt = now
            });
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

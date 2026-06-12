using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Notifications;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Notifications;

public class NotificationService(
    INotificationRepository notificationRepo,
    IValidator<CreateNotificationRequest> createValidator) : INotificationService
{
    private const int PageSize = 20;

    private readonly INotificationRepository _notificationRepo = notificationRepo;
    private readonly IValidator<CreateNotificationRequest> _createValidator = createValidator;

    public async Task<PagedResult<NotificationResponse>> GetMineAsync(NotificationListQuery query, Guid userId, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var (items, total) = await _notificationRepo.GetByUserAsync(userId, query.UnreadOnly, page, PageSize, cancellationToken);

        return new PagedResult<NotificationResponse>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = PageSize
        };
    }

    public Task<int> UnreadCountAsync(Guid userId, CancellationToken cancellationToken) =>
        _notificationRepo.UnreadCountAsync(userId, cancellationToken);

    public async Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        if (!await _notificationRepo.MarkReadAsync(userId, notificationId, cancellationToken))
            throw new NotFoundException($"Notification with id '{notificationId}' not found.");
    }

    public Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken) =>
        _notificationRepo.MarkAllReadAsync(userId, cancellationToken);

    public async Task<NotificationResponse> CreateForUserAsync(CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        if (!await _notificationRepo.UserExistsAsync(request.UserId, cancellationToken))
            throw new NotFoundException($"User with id '{request.UserId}' not found.");

        return await _notificationRepo.CreateAsync(
            request.UserId, request.Title, request.Message,
            request.Type.Trim().ToLowerInvariant(), request.Link, cancellationToken);
    }
}

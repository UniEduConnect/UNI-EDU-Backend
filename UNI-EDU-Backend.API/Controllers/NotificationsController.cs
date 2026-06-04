using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Notifications;
using UNI_EDU_Backend.Application.Services.Notifications;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    private readonly INotificationService _notificationService = notificationService;

    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] NotificationListQuery query, CancellationToken cancellationToken)
    {
        var userId = ReadCallerIdOrThrow();
        PagedResult<NotificationResponse> result = await _notificationService.GetMineAsync(query, userId, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<NotificationResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get notifications successfully",
            Data = result
        });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        var userId = ReadCallerIdOrThrow();
        int count = await _notificationService.UnreadCountAsync(userId, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<UnreadCountResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get unread count successfully",
            Data = new UnreadCountResponse { Count = count }
        });
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = ReadCallerIdOrThrow();
        await _notificationService.MarkReadAsync(userId, id, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Notification marked as read",
            Data = null
        });
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = ReadCallerIdOrThrow();
        await _notificationService.MarkAllReadAsync(userId, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "All notifications marked as read",
            Data = null
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        NotificationResponse result = await _notificationService.CreateForUserAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<NotificationResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Notification created successfully",
            Data = result
        });
    }

    private Guid ReadCallerIdOrThrow()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user identifier claim.");
        return userId;
    }

    public class UnreadCountResponse
    {
        public int Count { get; set; }
    }
}

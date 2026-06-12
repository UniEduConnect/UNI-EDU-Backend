using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.Chat;
using UNI_EDU_Backend.Application.Services.Chat;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[ApiController]
[Authorize]
public class ChatController(IChatService chatService) : ControllerBase
{
    private readonly IChatService _chatService = chatService;

    [HttpGet("/api/classes/{classId:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid classId, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        List<MessageResponse> result = await _chatService.GetMessagesAsync(classId, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<List<MessageResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get messages successfully",
            Data = result
        });
    }

    [HttpPost("/api/classes/{classId:guid}/messages")]
    public async Task<IActionResult> Send(Guid classId, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        MessageResponse result = await _chatService.SendMessageAsync(classId, request, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<MessageResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Message sent successfully",
            Data = result
        });
    }

    [HttpPatch("/api/classes/{classId:guid}/messages/read")]
    public async Task<IActionResult> MarkRead(Guid classId, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        await _chatService.MarkReadAsync(classId, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Messages marked as read",
            Data = null
        });
    }

    private (Guid UserId, string Role) ReadCallerOrThrow()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user identifier claim.");

        var role = User.FindFirst(ClaimTypes.Role)?.Value
            ?? throw new UnauthorizedAccessException("Missing role claim.");

        return (userId, role);
    }
}

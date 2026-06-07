using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Chats;
using UNI_EDU_Backend.Application.Services.Chats;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChatsController(IChatService chatService) : ControllerBase
{
    private readonly IChatService _chatService = chatService;

    // --- Class chat ---

    [HttpGet("class/{classId:guid}/messages")]
    [Authorize]
    public async Task<IActionResult> GetClassMessages(Guid classId, [FromQuery] int page, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        PagedResult<ChatMessageResponse> result =
            await _chatService.GetClassMessagesAsync(classId, page <= 0 ? 1 : page, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<ChatMessageResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get class messages successfully",
            Data = result
        });
    }

    [HttpPost("class/{classId:guid}/messages")]
    [Authorize]
    public async Task<IActionResult> SendClassMessage(Guid classId, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        ChatMessageResponse result = await _chatService.SendClassMessageAsync(classId, request, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<ChatMessageResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Message sent",
            Data = result
        });
    }

    [HttpPost("class/{classId:guid}/read")]
    [Authorize]
    public async Task<IActionResult> MarkClassRead(Guid classId, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        await _chatService.MarkClassReadAsync(classId, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Marked as read",
            Data = null
        });
    }

    // --- Parent ↔ tutor DM ---

    [HttpGet("dm/{contactId:guid}/messages")]
    [Authorize]
    public async Task<IActionResult> GetDmMessages(Guid contactId, [FromQuery] int page, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        PagedResult<DmMessageResponse> result =
            await _chatService.GetDmMessagesAsync(contactId, page <= 0 ? 1 : page, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<DmMessageResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get direct messages successfully",
            Data = result
        });
    }

    [HttpPost("dm/{contactId:guid}/messages")]
    [Authorize]
    public async Task<IActionResult> SendDmMessage(Guid contactId, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        DmMessageResponse result = await _chatService.SendDmMessageAsync(contactId, request, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<DmMessageResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Message sent",
            Data = result
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

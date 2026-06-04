using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.Chat;
using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.Services.Chat;
using UNI_EDU_Backend.Application.Services.Profile;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[ApiController]
[Authorize]
public class MeController(IProfileService profileService, IChatService chatService) : ControllerBase
{
    private readonly IProfileService _profileService = profileService;
    private readonly IChatService _chatService = chatService;

    // All of the caller's sessions across every class, optional date window.
    [HttpGet("/api/me/sessions")]
    public async Task<IActionResult> GetMySessions([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();
        List<SessionResponse> result = await _profileService.GetMySessionsAsync(userId, role, from, to, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<List<SessionResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get sessions successfully",
            Data = result
        });
    }

    // Chat sidebar roll-up.
    [HttpGet("/api/me/conversations")]
    public async Task<IActionResult> GetMyConversations(CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();
        List<ConversationResponse> result = await _chatService.GetMyConversationsAsync(userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<List<ConversationResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get conversations successfully",
            Data = result
        });
    }

    private (Guid UserId, string Role) ReadCallerOrThrow()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user identifier claim.");
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        return (userId, role);
    }
}

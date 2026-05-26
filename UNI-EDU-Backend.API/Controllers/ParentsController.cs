using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.Parents;
using UNI_EDU_Backend.Application.Services.Parents;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ParentsController(IParentService parentService) : ControllerBase
{
    private readonly IParentService _parentService = parentService;

    [HttpGet("me/children")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> GetMyChildren(CancellationToken cancellationToken)
    {
        var parentId = ReadCallerIdOrThrow();

        List<StudentSummaryResponse> result = await _parentService.GetMyChildrenAsync(parentId, cancellationToken);

        ApiResponse<List<StudentSummaryResponse>> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get children successfully",
            Data = result
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    private Guid ReadCallerIdOrThrow()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user identifier claim.");
        return userId;
    }
}

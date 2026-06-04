using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.Parents;
using UNI_EDU_Backend.Application.DTOs.Profile;
using UNI_EDU_Backend.Application.Services.Parents;
using UNI_EDU_Backend.Application.Services.Profile;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ParentsController(IParentService parentService, IProfileService profileService) : ControllerBase
{
    private readonly IParentService _parentService = parentService;
    private readonly IProfileService _profileService = profileService;

    [HttpGet("me")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var parentId = ReadCallerIdOrThrow();
        ParentProfileResponse result = await _profileService.GetMyParentProfileAsync(parentId, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<ParentProfileResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get parent profile successfully",
            Data = result
        });
    }

    [HttpPut("me")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateParentProfileRequest request, CancellationToken cancellationToken)
    {
        var parentId = ReadCallerIdOrThrow();
        ParentProfileResponse result = await _profileService.UpdateMyParentProfileAsync(parentId, request, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<ParentProfileResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Parent profile updated successfully",
            Data = result
        });
    }

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

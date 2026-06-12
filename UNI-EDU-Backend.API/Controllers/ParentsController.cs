using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Exams;
using UNI_EDU_Backend.Application.DTOs.Parents;
using UNI_EDU_Backend.Application.DTOs.Profile;
using UNI_EDU_Backend.Application.Services.Parents;
using UNI_EDU_Backend.Application.Services.Profile;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ParentsController(IParentService parentService, IProfileService profileService, IParentChildLinkService linkService) : ControllerBase
{
    private readonly IParentService _parentService = parentService;
    private readonly IProfileService _profileService = profileService;
    private readonly IParentChildLinkService _linkService = linkService;

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

    // A child's exam results (parent can view their own child's scores).
    [HttpGet("me/children/{childId:guid}/exams")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> GetChildExams(Guid childId, [FromQuery] int page, CancellationToken cancellationToken)
    {
        PagedResult<SubmissionResponse> result = await _parentService.GetChildExamsAsync(ReadCallerIdOrThrow(), childId, page, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<SubmissionResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get child exams successfully",
            Data = result
        });
    }

    // A child's monthly progress + per-subject scores (real analytics for the reports page).
    [HttpGet("me/children/{childId:guid}/progress")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> GetChildProgress(Guid childId, CancellationToken cancellationToken)
    {
        ChildProgressAnalyticsResponse result = await _parentService.GetChildProgressAsync(ReadCallerIdOrThrow(), childId, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<ChildProgressAnalyticsResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get child progress successfully",
            Data = result
        });
    }

    // Parent tops up a child's wallet (to fund tuition/escrow).
    [HttpPost("me/children/{childId:guid}/fund")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> FundChild(Guid childId, [FromBody] FundChildRequest request, CancellationToken cancellationToken)
    {
        await _parentService.FundChildAsync(ReadCallerIdOrThrow(), childId, request.Amount, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object?>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Đã nạp tiền cho con thành công.",
            Data = null
        });
    }

    // Parent requests to link to a student by email; the student must confirm.
    [HttpPost("me/children/link")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> LinkChild([FromBody] LinkChildRequest request, CancellationToken cancellationToken)
    {
        await _linkService.RequestLinkAsync(ReadCallerIdOrThrow(), request, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object?>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Đã gửi yêu cầu liên kết. Vui lòng chờ học sinh xác nhận.",
            Data = null
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
}

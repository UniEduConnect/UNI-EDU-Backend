using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.Parents;
using UNI_EDU_Backend.Application.DTOs.Profile;
using UNI_EDU_Backend.Application.DTOs.Tutors;
using UNI_EDU_Backend.Application.Services.Parents;
using UNI_EDU_Backend.Application.Services.Profile;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Student")]
public class StudentsController(IProfileService profileService, IParentChildLinkService linkService) : ControllerBase
{
    private readonly IProfileService _profileService = profileService;
    private readonly IParentChildLinkService _linkService = linkService;

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var studentId = ReadCallerIdOrThrow();
        StudentProfileResponse result = await _profileService.GetMyStudentProfileAsync(studentId, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<StudentProfileResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get student profile successfully",
            Data = result
        });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateStudentProfileRequest request, CancellationToken cancellationToken)
    {
        var studentId = ReadCallerIdOrThrow();
        StudentProfileResponse result = await _profileService.UpdateMyStudentProfileAsync(studentId, request, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<StudentProfileResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Student profile updated successfully",
            Data = result
        });
    }

    [HttpGet("me/availability")]
    public async Task<IActionResult> GetMyAvailability(CancellationToken cancellationToken)
    {
        List<AvailableSlotDto> result = await _profileService.GetMyStudentAvailabilityAsync(ReadCallerIdOrThrow(), cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<List<AvailableSlotDto>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get availability successfully",
            Data = result
        });
    }

    // Smart matching: weekly slots where this student AND the given tutor are both free.
    [HttpGet("me/common-slots/{tutorId:guid}")]
    public async Task<IActionResult> GetCommonSlots(Guid tutorId, CancellationToken cancellationToken)
    {
        List<AvailableSlotDto> result = await _profileService.GetCommonSlotsWithTutorAsync(ReadCallerIdOrThrow(), tutorId, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<List<AvailableSlotDto>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get common slots successfully",
            Data = result
        });
    }

    // AI-ranked study slots (with a reason per slot) between this student and the given tutor.
    [HttpGet("me/ai-slots/{tutorId:guid}")]
    public async Task<IActionResult> GetAiSlots(Guid tutorId, CancellationToken cancellationToken)
    {
        List<AiSlotSuggestionDto> result = await _profileService.GetAiRankedSlotsWithTutorAsync(ReadCallerIdOrThrow(), tutorId, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<List<AiSlotSuggestionDto>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get AI slot suggestions successfully",
            Data = result
        });
    }

    [HttpPut("me/availability")]
    public async Task<IActionResult> UpdateMyAvailability([FromBody] UpdateAvailabilityRequest request, CancellationToken cancellationToken)
    {
        List<AvailableSlotDto> result = await _profileService.UpdateMyStudentAvailabilityAsync(ReadCallerIdOrThrow(), request, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<List<AvailableSlotDto>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Availability updated successfully",
            Data = result
        });
    }

    // Pending parent-link requests addressed to this student.
    [HttpGet("me/parent-links")]
    public async Task<IActionResult> GetParentLinks(CancellationToken cancellationToken)
    {
        var result = await _linkService.GetPendingForStudentAsync(ReadCallerIdOrThrow(), cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<List<ParentLinkRequestResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get parent link requests successfully",
            Data = result
        });
    }

    [HttpPost("me/parent-links/{id:guid}/approve")]
    public async Task<IActionResult> ApproveParentLink(Guid id, CancellationToken cancellationToken)
    {
        await _linkService.RespondAsync(ReadCallerIdOrThrow(), id, true, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object?>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Đã chấp nhận liên kết phụ huynh.",
            Data = null
        });
    }

    [HttpPost("me/parent-links/{id:guid}/reject")]
    public async Task<IActionResult> RejectParentLink(Guid id, CancellationToken cancellationToken)
    {
        await _linkService.RespondAsync(ReadCallerIdOrThrow(), id, false, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object?>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Đã từ chối liên kết phụ huynh.",
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

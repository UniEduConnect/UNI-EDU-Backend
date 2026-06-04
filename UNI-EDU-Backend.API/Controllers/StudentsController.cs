using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.Profile;
using UNI_EDU_Backend.Application.DTOs.Tutors;
using UNI_EDU_Backend.Application.Services.Profile;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Student")]
public class StudentsController(IProfileService profileService) : ControllerBase
{
    private readonly IProfileService _profileService = profileService;

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

    private Guid ReadCallerIdOrThrow()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user identifier claim.");
        return userId;
    }
}

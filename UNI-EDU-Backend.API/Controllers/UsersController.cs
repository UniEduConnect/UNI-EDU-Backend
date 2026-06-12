using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.Profile;
using UNI_EDU_Backend.Application.DTOs.SendOTP;
using UNI_EDU_Backend.Application.Services.Profile;
using UNI_EDU_Backend.Application.Services.Users;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController(IUserService userService, IProfileService profileService) : ControllerBase
{
    private readonly IUserService _userService = userService;
    private readonly IProfileService _profileService = profileService;

    [HttpPost("check-phone")]
    public async Task<IActionResult> CheckPhoneNumber([FromBody] CheckPhoneUserRequest sendOTPUserRequest)
    {
        CheckPhoneUserResponse response = await _userService.CheckPhoneNumberAsync(sendOTPUserRequest);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<CheckPhoneUserResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Check phone number successfully",
            Data = response
        });
    }

    // Current user's identity + role-specific profile. Used by almost every authenticated page.
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userId = ReadCallerIdOrThrow();
        CurrentUserResponse result = await _profileService.GetMeAsync(userId, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<CurrentUserResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get current user successfully",
            Data = result
        });
    }

    // Update common User fields (fullname, phone).
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest request, CancellationToken cancellationToken)
    {
        var userId = ReadCallerIdOrThrow();
        CurrentUserResponse result = await _profileService.UpdateMeAsync(userId, request, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<CurrentUserResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Profile updated successfully",
            Data = result
        });
    }

    // The caller's personal schedule (sessions across their classes).
    [HttpGet("me/schedule")]
    [Authorize]
    public async Task<IActionResult> GetMySchedule(CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();
        List<ScheduleItemResponse> result = await _profileService.GetMyScheduleAsync(userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<List<ScheduleItemResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get schedule successfully",
            Data = result
        });
    }

    private Guid ReadCallerIdOrThrow() => ReadCallerOrThrow().UserId;

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

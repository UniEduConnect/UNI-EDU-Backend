using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Profile;
using UNI_EDU_Backend.Application.DTOs.Tutors;
using UNI_EDU_Backend.Application.Services.Profile;
using UNI_EDU_Backend.Application.Services.Tutors;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TutorsController(ITutorService tutorService, IProfileService profileService) : ControllerBase
{
    private readonly ITutorService _tutorService = tutorService;
    private readonly IProfileService _profileService = profileService;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] TutorSearchQuery query, CancellationToken cancellationToken)
    {
        PagedResult<TutorListingResponse> result = await _tutorService.SearchTutorsAsync(query, cancellationToken);

        ApiResponse<PagedResult<TutorListingResponse>> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get tutors successfully",
            Data = result
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        TutorProfileResponse profile = await _tutorService.GetTutorProfileAsync(id, cancellationToken);

        ApiResponse<TutorProfileResponse> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get tutor profile successfully",
            Data = profile
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    [HttpGet("{id:guid}/reviews")]
    public async Task<IActionResult> GetReviews(Guid id, [FromQuery] TutorReviewsQuery query, CancellationToken cancellationToken)
    {
        PagedResult<TutorReviewResponse> result = await _tutorService.GetTutorReviewsAsync(id, query, cancellationToken);

        ApiResponse<PagedResult<TutorReviewResponse>> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get tutor reviews successfully",
            Data = result
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    [HttpGet("me/bank-account")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> GetMyBankAccount(CancellationToken cancellationToken)
    {
        var tutorId = ReadCallerIdOrThrow();

        BankAccountResponse? result = await _tutorService.GetMyBankAccountAsync(tutorId, cancellationToken);

        ApiResponse<BankAccountResponse> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = result is null ? "No bank account saved." : "Get bank account successfully",
            Data = result
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    // POST acts as upsert — saves on first call, overwrites on subsequent calls.
    [HttpPost("me/bank-account")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> SaveMyBankAccount([FromBody] SaveBankAccountRequest request, CancellationToken cancellationToken)
    {
        var tutorId = ReadCallerIdOrThrow();

        BankAccountResponse result = await _tutorService.SaveMyBankAccountAsync(tutorId, request, cancellationToken);

        ApiResponse<BankAccountResponse> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Bank account saved successfully",
            Data = result
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    [HttpDelete("me/bank-account")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> DeleteMyBankAccount(CancellationToken cancellationToken)
    {
        var tutorId = ReadCallerIdOrThrow();

        await _tutorService.DeleteMyBankAccountAsync(tutorId, cancellationToken);

        ApiResponse<object> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Bank account deleted successfully",
            Data = null
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    // Public — the find-tutor / booking UI reads a tutor's weekly availability.
    [HttpGet("{id:guid}/availability")]
    public async Task<IActionResult> GetAvailability(Guid id, CancellationToken cancellationToken)
    {
        List<AvailableSlotDto> result = await _tutorService.GetTutorAvailabilityAsync(id, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<List<AvailableSlotDto>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get tutor availability successfully",
            Data = result
        });
    }

    // Tutor self-edit of their rich profile (bio, rate, subjects, certificates, ...).
    [HttpPut("me/profile")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateTutorProfileRequest request, CancellationToken cancellationToken)
    {
        var tutorId = ReadCallerIdOrThrow();

        CurrentUserResponse result = await _profileService.UpdateMyTutorProfileAsync(tutorId, request, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<CurrentUserResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Tutor profile updated successfully",
            Data = result
        });
    }

    // PUT acts as a full replace of the caller's own availability slots.
    [HttpPut("me/availability")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> UpdateMyAvailability([FromBody] UpdateAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var tutorId = ReadCallerIdOrThrow();

        List<AvailableSlotDto> result = await _tutorService.UpdateMyAvailabilityAsync(tutorId, request, cancellationToken);

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

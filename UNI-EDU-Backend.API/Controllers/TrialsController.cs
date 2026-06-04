using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Trials;
using UNI_EDU_Backend.Application.Services.Trials;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[ApiController]
public class TrialsController(ITrialService trialService) : ControllerBase
{
    private readonly ITrialService _trialService = trialService;

    // Student requests a trial lesson with a tutor.
    [HttpPost("/api/tutors/{tutorId:guid}/trial-requests")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Create(Guid tutorId, [FromBody] CreateTrialRequest request, CancellationToken cancellationToken)
    {
        var studentId = ReadCallerIdOrThrow();
        TrialResponse result = await _trialService.CreateAsync(tutorId, request, studentId, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<TrialResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Trial request created successfully",
            Data = result
        });
    }

    // The caller's own trial requests (student view).
    [HttpGet("/api/trials/me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMine([FromQuery] TrialListQuery query, CancellationToken cancellationToken)
    {
        var studentId = ReadCallerIdOrThrow();
        PagedResult<TrialResponse> result = await _trialService.GetMineAsync(query, studentId, cancellationToken);
        return Ok200(result);
    }

    // Trial requests addressed to the caller (tutor view).
    [HttpGet("/api/trials/incoming")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> GetIncoming([FromQuery] TrialListQuery query, CancellationToken cancellationToken)
    {
        var tutorId = ReadCallerIdOrThrow();
        PagedResult<TrialResponse> result = await _trialService.GetIncomingAsync(query, tutorId, cancellationToken);
        return Ok200(result);
    }

    [HttpPatch("/api/trials/{id:guid}/accept")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken)
    {
        var tutorId = ReadCallerIdOrThrow();
        TrialResponse result = await _trialService.RespondAsync(id, tutorId, accept: true, cancellationToken);
        return Ok200(result, "Trial request accepted");
    }

    [HttpPatch("/api/trials/{id:guid}/decline")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> Decline(Guid id, CancellationToken cancellationToken)
    {
        var tutorId = ReadCallerIdOrThrow();
        TrialResponse result = await _trialService.RespondAsync(id, tutorId, accept: false, cancellationToken);
        return Ok200(result, "Trial request declined");
    }

    private IActionResult Ok200<T>(T data, string message = "Success") where T : class =>
        StatusCode(StatusCodes.Status200OK, new ApiResponse<T>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = message,
            Data = data
        });

    private Guid ReadCallerIdOrThrow()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user identifier claim.");
        return userId;
    }
}

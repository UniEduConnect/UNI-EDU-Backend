using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Trials;
using UNI_EDU_Backend.Application.Services.Trials;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TrialsController(ITrialService trialService) : ControllerBase
{
    private readonly ITrialService _trialService = trialService;

    [HttpPost]
    [Authorize(Roles = "Student,Parent")]
    public async Task<IActionResult> Create([FromBody] CreateTrialRequest request, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        TrialResponse result = await _trialService.CreateAsync(userId, role, request, cancellationToken);

        ApiResponse<TrialResponse> apiResponse = new()
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Trial booking request created successfully",
            Data = result
        };

        return StatusCode(StatusCodes.Status201Created, apiResponse);
    }

    // Role-scoped:
    //   Tutor   -> inbox (trials addressed to this tutor)
    //   Student -> outbox (trials this student initiated)
    //   Parent  -> trials across all their children
    //   Admin   -> every trial
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyTrials([FromQuery] TrialListQuery query, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        PagedResult<TrialResponse> result = await _trialService.GetMyTrialsAsync(query, userId, role, cancellationToken);

        ApiResponse<PagedResult<TrialResponse>> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get trials successfully",
            Data = result
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    [HttpPatch("{id:guid}/accept")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken)
    {
        var (userId, _) = ReadCallerOrThrow();

        TrialResponse result = await _trialService.AcceptAsync(id, userId, cancellationToken);

        ApiResponse<TrialResponse> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Trial accepted",
            Data = result
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    [HttpPatch("{id:guid}/reject")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectTrialRequest request, CancellationToken cancellationToken)
    {
        var (userId, _) = ReadCallerOrThrow();

        TrialResponse result = await _trialService.RejectAsync(id, userId, request ?? new RejectTrialRequest(), cancellationToken);

        ApiResponse<TrialResponse> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Trial rejected",
            Data = result
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    [HttpPatch("{id:guid}/complete")]
    [Authorize(Roles = "Student,Parent")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteTrialRequest request, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        TrialResponse result = await _trialService.CompleteAsync(id, userId, role, request ?? new CompleteTrialRequest(), cancellationToken);

        ApiResponse<TrialResponse> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Trial completed",
            Data = result
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
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

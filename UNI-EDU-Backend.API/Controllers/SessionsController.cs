using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.DTOs.Sessions;
using UNI_EDU_Backend.Application.Services.Sessions;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SessionsController(ISessionService sessionService) : ControllerBase
{
    private readonly ISessionService _sessionService = sessionService;

    [HttpGet("/api/classes/{classId:guid}/sessions")]
    [Authorize]
    public async Task<IActionResult> GetByClass(Guid classId, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        List<SessionResponse> result = await _sessionService.GetClassSessionsAsync(classId, userId, role, cancellationToken);

        ApiResponse<List<SessionResponse>> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get sessions successfully",
            Data = result
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    // Tutor adds a teaching session to a class's schedule.
    [HttpPost("/api/classes/{classId:guid}/sessions")]
    [Authorize(Roles = "Tutor,Admin")]
    public async Task<IActionResult> Create(Guid classId, [FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        SessionResponse result = await _sessionService.CreateAsync(classId, request, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<SessionResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Đã thêm buổi dạy",
            Data = result
        });
    }

    [HttpPatch("{id:guid}/start")]
    [Authorize]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        SessionResponse result = await _sessionService.StartAsync(id, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<SessionResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Session started",
            Data = result
        });
    }

    [HttpPatch("{id:guid}/end")]
    [Authorize]
    public async Task<IActionResult> End(Guid id, [FromBody] EndSessionRequest request, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        SessionResponse result = await _sessionService.EndAsync(id, request, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<SessionResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Session ended, awaiting confirmation",
            Data = result
        });
    }

    [HttpPatch("{id:guid}/confirm")]
    [Authorize]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        SessionResponse result = await _sessionService.ConfirmAsync(id, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<SessionResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Session confirmed",
            Data = result
        });
    }

    [HttpPatch("{id:guid}/complete")]
    [Authorize(Roles = "Tutor,Admin")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        SessionResponse result = await _sessionService.CompleteAsync(id, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<SessionResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Đã hoàn thành buổi học",
            Data = result
        });
    }

    [HttpPatch("{id:guid}/rate")]
    [Authorize]
    public async Task<IActionResult> Rate(Guid id, [FromBody] RateSessionRequest request, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        SessionResponse result = await _sessionService.RateAsync(id, request, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<SessionResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Session rated",
            Data = result
        });
    }

    [HttpPost("{id:guid}/absence")]
    [Authorize]
    public async Task<IActionResult> RequestAbsence(Guid id, [FromBody] CreateAbsenceRequest request, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        SessionResponse result = await _sessionService.RequestAbsenceAsync(id, request, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<SessionResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Absence request created, awaiting approval",
            Data = result
        });
    }

    [HttpPatch("{id:guid}/absence/approve")]
    [Authorize]
    public async Task<IActionResult> ApproveAbsence(Guid id, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        SessionResponse result = await _sessionService.ApproveAbsenceAsync(id, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<SessionResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Absence approved, session cancelled",
            Data = result
        });
    }

    [HttpPatch("{id:guid}/absence/reject")]
    [Authorize]
    public async Task<IActionResult> RejectAbsence(Guid id, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        SessionResponse result = await _sessionService.RejectAbsenceAsync(id, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<SessionResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Absence declined, session kept",
            Data = result
        });
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

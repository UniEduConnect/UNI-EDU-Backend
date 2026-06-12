using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.AiTests;
using UNI_EDU_Backend.Application.Services.AiTests;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Tutor")]
public class AiTestController(IAiTestService service) : ControllerBase
{
    private readonly IAiTestService _service = service;

    // Generate an AI test for a subject (the tutor takes it to qualify for accepting a class/student).
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateAiTestRequest request, CancellationToken cancellationToken)
    {
        AiTestResponse result = await _service.GenerateAsync(ReadCallerIdOrThrow(), request, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<AiTestResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "AI test generated",
            Data = result
        });
    }

    // Submit answers and get the score (pass = >= 80%).
    [HttpPost("{attemptId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid attemptId, [FromBody] SubmitAiTestRequest request, CancellationToken cancellationToken)
    {
        AiTestResultResponse result = await _service.SubmitAsync(ReadCallerIdOrThrow(), attemptId, request, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<AiTestResultResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = result.Passed ? "Đạt" : "Chưa đạt",
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

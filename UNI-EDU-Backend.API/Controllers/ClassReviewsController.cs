using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.Reviews;
using UNI_EDU_Backend.Application.Services.Reviews;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[ApiController]
[Authorize]
public class ClassReviewsController(IReviewService reviewService) : ControllerBase
{
    private readonly IReviewService _reviewService = reviewService;

    // Student/parent submits a review of the class's tutor (one per class per reviewer).
    [HttpPost("/api/classes/{classId:guid}/review")]
    public async Task<IActionResult> Create(Guid classId, [FromBody] CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        ReviewResponse result = await _reviewService.CreateClassReviewAsync(classId, request, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<ReviewResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Review submitted successfully",
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

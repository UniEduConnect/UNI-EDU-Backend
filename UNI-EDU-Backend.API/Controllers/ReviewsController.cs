using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Reviews;
using UNI_EDU_Backend.Application.Services.Reviews;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")] // Review moderation lives in the office/admin portal.
public class ReviewsController(IReviewService reviewService) : ControllerBase
{
    private readonly IReviewService _reviewService = reviewService;

    [HttpGet]
    public async Task<IActionResult> GetForModeration([FromQuery] ReviewModerationListQuery query, CancellationToken cancellationToken)
    {
        PagedResult<ModerationReviewResponse> result = await _reviewService.GetForModerationAsync(query, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<ModerationReviewResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get reviews successfully",
            Data = result
        });
    }

    [HttpPatch("{id:int}/hide")]
    public async Task<IActionResult> Hide(int id, CancellationToken cancellationToken)
    {
        await _reviewService.SetHiddenAsync(id, true, cancellationToken);
        return Ok200("Review hidden");
    }

    [HttpPatch("{id:int}/unhide")]
    public async Task<IActionResult> Unhide(int id, CancellationToken cancellationToken)
    {
        await _reviewService.SetHiddenAsync(id, false, cancellationToken);
        return Ok200("Review unhidden");
    }

    private IActionResult Ok200(string message) =>
        StatusCode(StatusCodes.Status200OK, new ApiResponse<object>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = message,
            Data = null
        });
}

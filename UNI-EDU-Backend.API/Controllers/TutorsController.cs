using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Tutors;
using UNI_EDU_Backend.Application.Services.Tutors;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TutorsController(ITutorService tutorService) : ControllerBase
{
    private readonly ITutorService _tutorService = tutorService;

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
}

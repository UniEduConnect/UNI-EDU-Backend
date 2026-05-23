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
}

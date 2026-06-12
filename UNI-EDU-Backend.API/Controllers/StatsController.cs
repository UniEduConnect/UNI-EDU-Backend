using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.Stats;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/stats")]
[ApiController]
public class StatsController(IStatsRepository statsRepo) : ControllerBase
{
    private readonly IStatsRepository _statsRepo = statsRepo;

    // Public aggregate metrics for the landing page.
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicStats(CancellationToken cancellationToken)
    {
        PublicStatsResponse result = await _statsRepo.GetPublicStatsAsync(cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PublicStatsResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get public stats successfully",
            Data = result
        });
    }
}

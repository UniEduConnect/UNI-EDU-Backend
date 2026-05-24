using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.Services.Classes;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClassesController(IClassService classService) : ControllerBase
{
    private readonly IClassService _classService = classService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClassRequest request, CancellationToken cancellationToken)
    {
        // TODO: Catch studentId, parentId through token

        ClassResponse result = await _classService.CreateClassAsync(request, cancellationToken);

        ApiResponse<ClassResponse> apiResponse = new()
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Class created successfully",
            Data = result
        };

        return StatusCode(StatusCodes.Status201Created, apiResponse);
    }
}

using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.Subjects;
using UNI_EDU_Backend.Application.Services.Subjects;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SubjectsController(ISubjectService subjectService) : ControllerBase
{
    private readonly ISubjectService _subjectService = subjectService;

    // Public — used for subject dropdowns across the app (tutor search, exam/question forms).
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        List<SubjectResponse> result = await _subjectService.GetAllAsync(cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<List<SubjectResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get subjects successfully",
            Data = result
        });
    }
}

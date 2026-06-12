using Microsoft.AspNetCore.Authorization;
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

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] SaveSubjectRequest request, CancellationToken cancellationToken)
    {
        SubjectResponse result = await _subjectService.CreateAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<SubjectResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Subject created successfully",
            Data = result
        });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveSubjectRequest request, CancellationToken cancellationToken)
    {
        SubjectResponse result = await _subjectService.UpdateAsync(id, request, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<SubjectResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Subject updated successfully",
            Data = result
        });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _subjectService.DeleteAsync(id, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Subject deleted successfully",
            Data = null
        });
    }
}

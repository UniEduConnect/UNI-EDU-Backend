using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Questions;
using UNI_EDU_Backend.Application.Services.Questions;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")] // Question bank lives in the exam-manager (Admin) portal.
public class QuestionsController(IQuestionService questionService) : ControllerBase
{
    private readonly IQuestionService _questionService = questionService;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] QuestionListQuery query, CancellationToken cancellationToken)
    {
        PagedResult<QuestionResponse> result = await _questionService.SearchAsync(query, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<QuestionResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get questions successfully",
            Data = result
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        QuestionResponse result = await _questionService.GetByIdAsync(id, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<QuestionResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get question successfully",
            Data = result
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuestionRequest request, CancellationToken cancellationToken)
    {
        QuestionResponse result = await _questionService.CreateAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<QuestionResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Question created successfully",
            Data = result
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateQuestionRequest request, CancellationToken cancellationToken)
    {
        QuestionResponse result = await _questionService.UpdateAsync(id, request, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<QuestionResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Question updated successfully",
            Data = result
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _questionService.DeleteAsync(id, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Question deleted successfully",
            Data = null
        });
    }
}

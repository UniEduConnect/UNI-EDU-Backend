using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Exams;
using UNI_EDU_Backend.Application.Services.Exams;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ExamsController(IExamService examService) : ControllerBase
{
    private readonly IExamService _examService = examService;

    // Authenticated listing. Admin sees every exam (incl. drafts/hidden); others see open/closed only.
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Search([FromQuery] ExamListQuery query, CancellationToken cancellationToken)
    {
        var role = ReadRoleOrThrow();
        PagedResult<ExamListItemResponse> result = await _examService.SearchAsync(query, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<ExamListItemResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get exams successfully",
            Data = result
        });
    }

    // Exam detail incl. ordered questions. Non-admins get the answer key stripped (take-exam view).
    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var role = ReadRoleOrThrow();
        ExamDetailResponse result = await _examService.GetByIdAsync(id, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<ExamDetailResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get exam successfully",
            Data = result
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateExamRequest request, CancellationToken cancellationToken)
    {
        ExamDetailResponse result = await _examService.CreateAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<ExamDetailResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Exam created successfully",
            Data = result
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateExamRequest request, CancellationToken cancellationToken)
    {
        ExamDetailResponse result = await _examService.UpdateAsync(id, request, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<ExamDetailResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Exam updated successfully",
            Data = result
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _examService.DeleteAsync(id, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Exam deleted successfully",
            Data = null
        });
    }

    // Replace the exam's full ordered question set.
    [HttpPut("{id:int}/questions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetQuestions(int id, [FromBody] SetExamQuestionsRequest request, CancellationToken cancellationToken)
    {
        ExamDetailResponse result = await _examService.SetQuestionsAsync(id, request, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<ExamDetailResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Exam questions updated successfully",
            Data = result
        });
    }

    // Submit answers; auto-graded for multiple-choice questions.
    [HttpPost("{id:int}/submit")]
    [Authorize]
    public async Task<IActionResult> Submit(int id, [FromBody] SubmitExamRequest request, CancellationToken cancellationToken)
    {
        var userId = ReadUserIdOrThrow();
        SubmissionResponse result = await _examService.SubmitAsync(id, request, userId, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<SubmissionResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Exam submitted successfully",
            Data = result
        });
    }

    [HttpGet("ai-config")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAiConfig(CancellationToken cancellationToken)
    {
        ExamAiConfigResponse result = await _examService.GetAiConfigAsync(cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<ExamAiConfigResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get AI config successfully",
            Data = result
        });
    }

    [HttpPut("ai-config")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAiConfig([FromBody] UpdateExamAiConfigRequest request, CancellationToken cancellationToken)
    {
        ExamAiConfigResponse result = await _examService.UpdateAiConfigAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<ExamAiConfigResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "AI config updated successfully",
            Data = result
        });
    }

    // Per-exam aggregate analytics (exam-manager stats screen).
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        List<ExamStatItem> result = await _examService.GetStatsAsync(cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<List<ExamStatItem>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get exam stats successfully",
            Data = result
        });
    }

    // All attempts for an exam (exam-manager analytics). Available as both /submissions and /attempts.
    [HttpGet("{id:int}/submissions")]
    [HttpGet("{id:int}/attempts")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetExamSubmissions(int id, [FromQuery] int page, CancellationToken cancellationToken)
    {
        PagedResult<SubmissionResponse> result = await _examService.GetExamSubmissionsAsync(id, page < 1 ? 1 : page, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<SubmissionResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get exam submissions successfully",
            Data = result
        });
    }

    // The caller's own attempt history (student results page).
    [HttpGet("me/submissions")]
    [Authorize]
    public async Task<IActionResult> GetMySubmissions([FromQuery] int page, CancellationToken cancellationToken)
    {
        var userId = ReadUserIdOrThrow();
        PagedResult<SubmissionResponse> result = await _examService.GetMySubmissionsAsync(userId, page < 1 ? 1 : page, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<SubmissionResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get my submissions successfully",
            Data = result
        });
    }

    private string ReadRoleOrThrow() =>
        User.FindFirst(ClaimTypes.Role)?.Value
            ?? throw new UnauthorizedAccessException("Missing role claim.");

    private Guid ReadUserIdOrThrow()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user identifier claim.");
        return userId;
    }
}

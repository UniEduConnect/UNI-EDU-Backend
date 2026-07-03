using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.TutorPosts;
using UNI_EDU_Backend.Application.Services.TutorPosts;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TutorPostsController(ITutorPostService service) : ControllerBase
{
    private readonly ITutorPostService _service = service;

    // Tutor posts a "looking for students" ad.
    [HttpPost]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> Create([FromBody] CreateTutorPostRequest request, CancellationToken cancellationToken)
    {
        await _service.CreateAsync(ReadCallerIdOrThrow(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<object?>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Đã đăng tin tìm học sinh.",
            Data = null
        });
    }

    // Students/parents browse open tutor posts (tutors must not see other tutors' ads).
    [HttpGet("open")]
    [Authorize(Roles = "Student,Parent")]
    public async Task<IActionResult> GetOpen([FromQuery] TutorPostListQuery query, CancellationToken cancellationToken)
    {
        PagedResult<TutorPostResponse> result = await _service.GetOpenAsync(query, ReadCallerIdOrThrow(), cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<TutorPostResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get open tutor posts successfully",
            Data = result
        });
    }

    // A tutor's own posts.
    [HttpGet("me")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        List<TutorPostResponse> result = await _service.GetMineAsync(ReadCallerIdOrThrow(), cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<List<TutorPostResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get my tutor posts successfully",
            Data = result
        });
    }

    [HttpPatch("{id:guid}/close")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        await _service.CloseAsync(ReadCallerIdOrThrow(), id, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object?>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Đã đóng tin đăng.",
            Data = null
        });
    }

    // Student registers ("đăng ký học") on a tutor's post.
    [HttpPost("{id:guid}/apply")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Apply(Guid id, CancellationToken cancellationToken)
    {
        await _service.ApplyAsync(ReadCallerIdOrThrow(), id, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object?>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Đã đăng ký học. Gia sư sẽ làm bài test để nhận bạn.",
            Data = null
        });
    }

    // Tutor's pending applications (students who registered on their posts).
    [HttpGet("applications")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> GetApplications(CancellationToken cancellationToken)
    {
        List<TutorPostApplicationResponse> result = await _service.GetApplicationsAsync(ReadCallerIdOrThrow(), cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<List<TutorPostApplicationResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get applications successfully",
            Data = result
        });
    }

    // Tutor accepts an application after passing an AI test (>=80%) for the post subject.
    [HttpPost("applications/{appId:guid}/accept")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> AcceptApplication(Guid appId, [FromBody] AcceptApplicationRequest request, CancellationToken cancellationToken)
    {
        await _service.AcceptApplicationAsync(ReadCallerIdOrThrow(), appId, request, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object?>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Đã nhận học sinh thành công.",
            Data = null
        });
    }

    private Guid ReadCallerIdOrThrow()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user identifier claim.");
        return userId;
    }
}

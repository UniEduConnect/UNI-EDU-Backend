using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.ClassRequests;
using UNI_EDU_Backend.Application.Services.ClassRequests;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClassRequestsController(IClassRequestService service) : ControllerBase
{
    private readonly IClassRequestService _service = service;

    // Student posts a "looking for a tutor" request; a Parent may post for a linked child.
    [HttpPost]
    [Authorize(Roles = "Student,Parent")]
    public async Task<IActionResult> Create([FromBody] CreateClassRequestRequest request, CancellationToken cancellationToken)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.Equals(role, "Parent", StringComparison.OrdinalIgnoreCase))
            await _service.CreateForParentAsync(ReadCallerIdOrThrow(), request, cancellationToken);
        else
            await _service.CreateAsync(ReadCallerIdOrThrow(), request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<object?>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Đã đăng yêu cầu tìm gia sư.",
            Data = null
        });
    }

    // Tutor/Teacher browses open requests ("find students").
    [HttpGet("open")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> GetOpen([FromQuery] ClassRequestListQuery query, CancellationToken cancellationToken)
    {
        PagedResult<ClassRequestResponse> result = await _service.GetOpenAsync(query, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<ClassRequestResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get open class requests successfully",
            Data = result
        });
    }

    // A student's own posts.
    [HttpGet("me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        List<ClassRequestResponse> result = await _service.GetMineAsync(ReadCallerIdOrThrow(), cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<List<ClassRequestResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get my class requests successfully",
            Data = result
        });
    }

    // Read-only pre-check the UI calls BEFORE opening the AI test: 200 if the tutor could accept
    // this request (open, no schedule clash, student can pay), otherwise the typed exception's status.
    [HttpGet("{id:guid}/accept-check")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> AcceptCheck(Guid id, CancellationToken cancellationToken)
    {
        await _service.EnsureAcceptableAsync(ReadCallerIdOrThrow(), id, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object?>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Có thể nhận lớp.",
            Data = null
        });
    }

    // Tutor accepts a request — requires a passing tutor-test submission (per acceptance).
    [HttpPost("{id:guid}/accept")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> Accept(Guid id, [FromBody] AcceptClassRequestRequest request, CancellationToken cancellationToken)
    {
        await _service.AcceptAsync(ReadCallerIdOrThrow(), id, request, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object?>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Đã nhận lớp thành công.",
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

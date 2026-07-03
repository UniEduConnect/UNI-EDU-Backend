using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Refunds;
using UNI_EDU_Backend.Application.Services.Refunds;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[ApiController]
public class RefundsController(IRefundService refundService) : ControllerBase
{
    private readonly IRefundService _refundService = refundService;

    // Student/parent requests a refund of remaining escrow on a class.
    [HttpPost("/api/Classes/{classId:guid}/refund-request")]
    [Authorize(Roles = "Student,Parent")]
    public async Task<IActionResult> Create(Guid classId, [FromBody] CreateRefundRequest request, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();
        RefundResponse result = await _refundService.CreateAsync(classId, request, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<RefundResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Refund request created successfully",
            Data = result
        });
    }

    // A tutor's own refund history — refunds raised on classes they teach.
    [HttpGet("/api/me/refunds")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> GetMine([FromQuery] RefundListQuery query, CancellationToken cancellationToken)
    {
        var (userId, _) = ReadCallerOrThrow();
        PagedResult<RefundResponse> result = await _refundService.GetForTutorAsync(userId, query, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<RefundResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get my refunds successfully",
            Data = result
        });
    }

    // Finance portal (Admin role).
    [HttpGet("/api/Finance/refunds")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] RefundListQuery query, CancellationToken cancellationToken)
    {
        PagedResult<RefundResponse> result = await _refundService.GetAllAsync(query, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<RefundResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get refunds successfully",
            Data = result
        });
    }

    [HttpPost("/api/Finance/refunds/{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ReviewRefundRequest request, CancellationToken cancellationToken)
    {
        var (userId, _) = ReadCallerOrThrow();
        await _refundService.ApproveAsync(id, request ?? new ReviewRefundRequest(), userId, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Refund approved successfully",
            Data = null
        });
    }

    [HttpPost("/api/Finance/refunds/{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ReviewRefundRequest request, CancellationToken cancellationToken)
    {
        var (userId, _) = ReadCallerOrThrow();
        await _refundService.RejectAsync(id, request ?? new ReviewRefundRequest(), userId, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Refund rejected successfully",
            Data = null
        });
    }

    private (Guid UserId, string Role) ReadCallerOrThrow()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user identifier claim.");
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        return (userId, role);
    }
}

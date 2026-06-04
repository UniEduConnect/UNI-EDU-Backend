using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Admin;
using UNI_EDU_Backend.Application.Services.Admin;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    private readonly IAdminService _adminService = adminService;

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] AdminUserListQuery query, CancellationToken cancellationToken)
    {
        PagedResult<AdminUserResponse> result = await _adminService.GetUsersAsync(query, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<AdminUserResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get users successfully",
            Data = result
        });
    }

    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        AdminUserResponse result = await _adminService.GetUserByIdAsync(id, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<AdminUserResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get user successfully",
            Data = result
        });
    }

    [HttpPost("users/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var adminId = ReadCallerIdOrThrow();
        AdminUserResponse result = await _adminService.ApproveUserAsync(id, adminId, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<AdminUserResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "User approved successfully",
            Data = result
        });
    }

    [HttpPost("users/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectUserRequest request, CancellationToken cancellationToken)
    {
        var adminId = ReadCallerIdOrThrow();
        AdminUserResponse result = await _adminService.RejectUserAsync(id, request ?? new RejectUserRequest(), adminId, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<AdminUserResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "User rejected successfully",
            Data = result
        });
    }

    [HttpPost("users/{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken cancellationToken)
    {
        var adminId = ReadCallerIdOrThrow();
        AdminUserResponse result = await _adminService.SuspendUserAsync(id, adminId, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<AdminUserResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "User suspended successfully",
            Data = result
        });
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int page, CancellationToken cancellationToken)
    {
        PagedResult<AuditLogResponse> result = await _adminService.GetAuditLogsAsync(page < 1 ? 1 : page, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<AuditLogResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get audit logs successfully",
            Data = result
        });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        DashboardResponse result = await _adminService.GetDashboardAsync(cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<DashboardResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get dashboard successfully",
            Data = result
        });
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        SystemSettingsResponse result = await _adminService.GetSettingsAsync(cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<SystemSettingsResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get settings successfully",
            Data = result
        });
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSystemSettingsRequest request, CancellationToken cancellationToken)
    {
        var adminId = ReadCallerIdOrThrow();
        SystemSettingsResponse result = await _adminService.UpdateSettingsAsync(request, adminId, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<SystemSettingsResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Settings updated successfully",
            Data = result
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

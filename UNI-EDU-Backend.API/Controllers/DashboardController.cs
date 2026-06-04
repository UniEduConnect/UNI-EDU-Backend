using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Services.Dashboards;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

// Per-portal summary KPI cards. Routes match the FE contract exactly.
[ApiController]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    private readonly IDashboardService _dashboardService = dashboardService;

    [HttpGet("/api/Tutors/me/dashboard")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> Tutor(CancellationToken cancellationToken) =>
        Ok200(await _dashboardService.GetTutorDashboardAsync(ReadCallerIdOrThrow(), cancellationToken));

    [HttpGet("/api/Students/me/dashboard")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Student(CancellationToken cancellationToken) =>
        Ok200(await _dashboardService.GetStudentDashboardAsync(ReadCallerIdOrThrow(), cancellationToken));

    [HttpGet("/api/Parents/me/dashboard")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> Parent(CancellationToken cancellationToken) =>
        Ok200(await _dashboardService.GetParentDashboardAsync(ReadCallerIdOrThrow(), cancellationToken));

    [HttpGet("/api/Finance/dashboard")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Finance(CancellationToken cancellationToken) =>
        Ok200(await _dashboardService.GetFinanceDashboardAsync(cancellationToken));

    [HttpGet("/api/Office/dashboard")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Office(CancellationToken cancellationToken) =>
        Ok200(await _dashboardService.GetOfficeDashboardAsync(cancellationToken));

    [HttpGet("/api/Exams/dashboard")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Exams(CancellationToken cancellationToken) =>
        Ok200(await _dashboardService.GetExamsDashboardAsync(cancellationToken));

    private IActionResult Ok200<T>(T data) where T : class =>
        StatusCode(StatusCodes.Status200OK, new ApiResponse<T>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get dashboard successfully",
            Data = data
        });

    private Guid ReadCallerIdOrThrow()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user identifier claim.");
        return userId;
    }
}

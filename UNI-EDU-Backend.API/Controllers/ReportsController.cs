using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Services.Reports;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[ApiController]
public class ReportsController(IReportService reportService) : ControllerBase
{
    private readonly IReportService _reportService = reportService;

    [HttpGet("/api/Admin/reports")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Admin(CancellationToken cancellationToken) =>
        Ok200(await _reportService.GetAdminReportAsync(cancellationToken));

    [HttpGet("/api/Finance/reports")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Finance(CancellationToken cancellationToken) =>
        Ok200(await _reportService.GetFinanceReportAsync(cancellationToken));

    [HttpGet("/api/Office/reports")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Office(CancellationToken cancellationToken) =>
        Ok200(await _reportService.GetOfficeReportAsync(cancellationToken));

    [HttpGet("/api/Students/me/report")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> StudentMe(CancellationToken cancellationToken) =>
        Ok200(await _reportService.GetStudentReportAsync(ReadCallerIdOrThrow(), cancellationToken));

    [HttpGet("/api/Parents/me/report")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> ParentMe(CancellationToken cancellationToken) =>
        Ok200(await _reportService.GetParentReportAsync(ReadCallerIdOrThrow(), cancellationToken));

    private IActionResult Ok200<T>(T data) where T : class =>
        StatusCode(StatusCodes.Status200OK, new ApiResponse<T>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get report successfully",
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

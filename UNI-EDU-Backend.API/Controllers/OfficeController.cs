using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Office;
using UNI_EDU_Backend.Application.Services.Office;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")] // Office portal maps to the Admin role in the backend.
public class OfficeController(IOfficeService officeService) : ControllerBase
{
    private readonly IOfficeService _officeService = officeService;

    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendance([FromQuery] AttendanceListQuery query, CancellationToken cancellationToken)
    {
        PagedResult<AttendanceResponse> result = await _officeService.GetAttendanceAsync(query, cancellationToken);
        return Ok200(result, "Get attendance successfully");
    }

    [HttpGet("attendance/{sessionId:guid}")]
    public async Task<IActionResult> GetAttendanceById(Guid sessionId, CancellationToken cancellationToken)
    {
        AttendanceResponse result = await _officeService.GetAttendanceByIdAsync(sessionId, cancellationToken);
        return Ok200(result, "Get attendance successfully");
    }

    [HttpPatch("attendance/{sessionId:guid}/confirm")]
    public async Task<IActionResult> ConfirmAttendance(Guid sessionId, CancellationToken cancellationToken)
    {
        await _officeService.ConfirmAttendanceAsync(sessionId, cancellationToken);
        return Ok200<object>(null, "Attendance confirmed");
    }

    [HttpGet("incidents")]
    public async Task<IActionResult> GetIncidents([FromQuery] IncidentListQuery query, CancellationToken cancellationToken)
    {
        PagedResult<IncidentResponse> result = await _officeService.GetIncidentsAsync(query, cancellationToken);
        return Ok200(result, "Get incidents successfully");
    }

    [HttpPost("incidents")]
    public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentRequest request, CancellationToken cancellationToken)
    {
        var (userId, role, name) = ReadCallerOrThrow();
        IncidentResponse result = await _officeService.CreateIncidentAsync(request, userId, name, role, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<IncidentResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Incident reported successfully",
            Data = result
        });
    }

    [HttpPatch("incidents/{id:guid}/investigate")]
    public async Task<IActionResult> Investigate(Guid id, CancellationToken cancellationToken)
    {
        await _officeService.InvestigateIncidentAsync(id, cancellationToken);
        return Ok200<object>(null, "Incident marked as investigating");
    }

    [HttpPatch("incidents/{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveIncidentRequest request, CancellationToken cancellationToken)
    {
        await _officeService.ResolveIncidentAsync(id, request ?? new ResolveIncidentRequest(), cancellationToken);
        return Ok200<object>(null, "Incident resolved");
    }

    [HttpGet("registrations")]
    public async Task<IActionResult> GetRegistrations([FromQuery] int page, CancellationToken cancellationToken)
    {
        var result = await _officeService.GetRegistrationsAsync(page < 1 ? 1 : page, cancellationToken);
        return Ok200(result, "Get registrations successfully");
    }

    [HttpPost("registrations/{id:guid}/approve")]
    public async Task<IActionResult> ApproveRegistration(Guid id, CancellationToken cancellationToken)
    {
        var (userId, _, _) = ReadCallerOrThrow();
        await _officeService.ApproveRegistrationAsync(id, userId, cancellationToken);
        return Ok200<object>(null, "Registration approved");
    }

    [HttpPost("registrations/{id:guid}/reject")]
    public async Task<IActionResult> RejectRegistration(Guid id, CancellationToken cancellationToken)
    {
        var (userId, _, _) = ReadCallerOrThrow();
        await _officeService.RejectRegistrationAsync(id, userId, cancellationToken);
        return Ok200<object>(null, "Registration rejected");
    }

    [HttpGet("appointments")]
    public async Task<IActionResult> GetAppointments([FromQuery] AppointmentListQuery query, CancellationToken cancellationToken)
    {
        var result = await _officeService.GetAppointmentsAsync(query, cancellationToken);
        return Ok200(result, "Get appointments successfully");
    }

    [HttpPost("appointments")]
    public async Task<IActionResult> CreateAppointment([FromBody] SaveAppointmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _officeService.CreateAppointmentAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<AppointmentResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Appointment created successfully",
            Data = result
        });
    }

    [HttpPut("appointments/{id:guid}")]
    public async Task<IActionResult> UpdateAppointment(Guid id, [FromBody] SaveAppointmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _officeService.UpdateAppointmentAsync(id, request, cancellationToken);
        return Ok200(result, "Appointment updated successfully");
    }

    [HttpPatch("appointments/{id:guid}/cancel")]
    public async Task<IActionResult> CancelAppointment(Guid id, CancellationToken cancellationToken)
    {
        await _officeService.CancelAppointmentAsync(id, cancellationToken);
        return Ok200<object>(null, "Appointment cancelled");
    }

    [HttpPatch("appointments/{id:guid}/complete")]
    public async Task<IActionResult> CompleteAppointment(Guid id, CancellationToken cancellationToken)
    {
        await _officeService.CompleteAppointmentAsync(id, cancellationToken);
        return Ok200<object>(null, "Appointment completed");
    }

    [HttpPost("ai-schedule")]
    public IActionResult GenerateSchedule([FromBody] AiScheduleRequest request)
    {
        var result = _officeService.GenerateSchedule(request ?? new AiScheduleRequest());
        return Ok200(result, "Schedule generated");
    }

    private IActionResult Ok200<T>(T? data, string message) where T : class =>
        StatusCode(StatusCodes.Status200OK, new ApiResponse<T>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = message,
            Data = data
        });

    private (Guid UserId, string Role, string Name) ReadCallerOrThrow()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user identifier claim.");

        var role = User.FindFirst(ClaimTypes.Role)?.Value
            ?? throw new UnauthorizedAccessException("Missing role claim.");

        // No name claim in the JWT — fall back to the email claim as the reporter label.
        var name = User.FindFirst(ClaimTypes.Email)?.Value ?? role;

        return (userId, role, name);
    }
}

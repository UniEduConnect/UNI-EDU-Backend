using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Office;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Services.Office;

public class OfficeService(
    IOfficeRepository officeRepo,
    IValidator<CreateIncidentRequest> createIncidentValidator) : IOfficeService
{
    private const int PageSize = 20;

    private readonly IOfficeRepository _officeRepo = officeRepo;
    private readonly IValidator<CreateIncidentRequest> _createIncidentValidator = createIncidentValidator;

    public async Task<PagedResult<AttendanceResponse>> GetAttendanceAsync(AttendanceListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var (items, total) = await _officeRepo.GetAttendanceAsync(query.Status, page, PageSize, cancellationToken);

        return new PagedResult<AttendanceResponse>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = PageSize
        };
    }

    public async Task<AttendanceResponse> GetAttendanceByIdAsync(Guid sessionId, CancellationToken cancellationToken) =>
        await _officeRepo.GetAttendanceByIdAsync(sessionId, cancellationToken)
            ?? throw new NotFoundException($"Session with id '{sessionId}' not found.");

    public async Task ConfirmAttendanceAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        if (!await _officeRepo.ConfirmAttendanceAsync(sessionId, cancellationToken))
            throw new NotFoundException($"Session with id '{sessionId}' not found.");
    }

    public async Task<PagedResult<IncidentResponse>> GetIncidentsAsync(IncidentListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var (items, total) = await _officeRepo.GetIncidentsAsync(ParseStatus(query.Status), page, PageSize, cancellationToken);

        return new PagedResult<IncidentResponse>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = PageSize
        };
    }

    public async Task<IncidentResponse> CreateIncidentAsync(CreateIncidentRequest request, Guid reporterId, string reporterName, string reporterRole, CancellationToken cancellationToken)
    {
        await _createIncidentValidator.EnsureValidAsync(request, cancellationToken);

        if (!await _officeRepo.ClassExistsAsync(request.ClassId, cancellationToken))
            throw new NotFoundException($"Class with id '{request.ClassId}' not found.");

        if (request.SessionId is Guid sessionId &&
            !await _officeRepo.SessionBelongsToClassAsync(sessionId, request.ClassId, cancellationToken))
            throw new BadRequestException("The session does not belong to the specified class.");

        return await _officeRepo.CreateIncidentAsync(
            request.ClassId, request.SessionId, reporterId, reporterName, reporterRole,
            request.Description, ParsePriority(request.Priority), cancellationToken);
    }

    public async Task InvestigateIncidentAsync(Guid incidentId, CancellationToken cancellationToken)
    {
        if (await _officeRepo.SetIncidentStatusAsync(incidentId, IncidentStatus.Investigating, null, cancellationToken) == IncidentReviewOutcome.NotFound)
            throw new NotFoundException($"Incident with id '{incidentId}' not found.");
    }

    public async Task ResolveIncidentAsync(Guid incidentId, ResolveIncidentRequest request, CancellationToken cancellationToken)
    {
        if (await _officeRepo.SetIncidentStatusAsync(incidentId, IncidentStatus.Resolved, request.Resolution, cancellationToken) == IncidentReviewOutcome.NotFound)
            throw new NotFoundException($"Incident with id '{incidentId}' not found.");
    }

    private static IncidentPriority ParsePriority(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "low" => IncidentPriority.Low,
            "high" => IncidentPriority.High,
            _ => IncidentPriority.Medium
        };

    private static IncidentStatus? ParseStatus(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "pending" => IncidentStatus.Pending,
            "investigating" => IncidentStatus.Investigating,
            "resolved" => IncidentStatus.Resolved,
            _ => null
        };
}

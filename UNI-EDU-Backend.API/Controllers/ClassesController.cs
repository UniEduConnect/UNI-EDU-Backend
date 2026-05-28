using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.Services.Classes;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClassesController(IClassService classService) : ControllerBase
{
    private readonly IClassService _classService = classService;

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateClassRequest request, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        ClassResponse result = await _classService.CreateClassAsync(request, userId, role, cancellationToken);

        ApiResponse<ClassResponse> apiResponse = new()
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Class created successfully",
            Data = result
        };

        return StatusCode(StatusCodes.Status201Created, apiResponse);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyClasses([FromQuery] ClassListQuery query, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        PagedResult<ClassListItemResponse> result = await _classService.GetMyClassesAsync(query, userId, role, cancellationToken);

        ApiResponse<PagedResult<ClassListItemResponse>> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get classes successfully",
            Data = result
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        ClassDetailResponse result = await _classService.GetClassByIdAsync(id, userId, role, cancellationToken);

        ApiResponse<ClassDetailResponse> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get class successfully",
            Data = result
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClassRequest request, CancellationToken cancellationToken)
    {
        ClassDetailResponse result = await _classService.UpdateClassAsync(id, request, cancellationToken);

        ApiResponse<ClassDetailResponse> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Class updated successfully",
            Data = result
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    private (Guid UserId, string Role) ReadCallerOrThrow()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user identifier claim.");

        var role = User.FindFirst(ClaimTypes.Role)?.Value
            ?? throw new UnauthorizedAccessException("Missing role claim.");

        return (userId, role);
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.Services.Materials;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[ApiController]
[Authorize]
public class ClassMaterialsController(IMaterialService materialService) : ControllerBase
{
    private readonly IMaterialService _materialService = materialService;

    [HttpGet("/api/classes/{classId:guid}/materials")]
    public async Task<IActionResult> GetByClass(Guid classId, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        List<MaterialResponse> result = await _materialService.GetClassMaterialsAsync(classId, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<List<MaterialResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get materials successfully",
            Data = result
        });
    }

    [HttpPost("/api/classes/{classId:guid}/materials")]
    public async Task<IActionResult> Add(Guid classId, [FromBody] CreateMaterialRequest request, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        MaterialResponse result = await _materialService.AddMaterialAsync(classId, request, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<MaterialResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Material added successfully",
            Data = result
        });
    }

    [HttpDelete("/api/classes/{classId:guid}/materials/{materialId:guid}")]
    public async Task<IActionResult> Delete(Guid classId, Guid materialId, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        await _materialService.DeleteMaterialAsync(classId, materialId, userId, role, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Material deleted successfully",
            Data = null
        });
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

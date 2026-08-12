using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // any authenticated user may upload (avatars, materials, ...)
public class UploadsController(IFileStorageService storage) : ControllerBase
{
    private readonly IFileStorageService _storage = storage;

    private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly HashSet<string> AllowedImageTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp", "image/gif" };

    // POST /api/uploads/image — multipart/form-data with a "file" field. Returns the public URL.
    [HttpPost("image")]
    [RequestSizeLimit(MaxImageBytes + 1024)]
    public async Task<IActionResult> UploadImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            throw new BadRequestException("No file was uploaded.");
        if (file.Length > MaxImageBytes)
            throw new BadRequestException("Image must be 5 MB or smaller.");
        if (!AllowedImageTypes.Contains(file.ContentType))
            throw new BadRequestException("Only JPEG, PNG, WebP or GIF images are allowed.");

        await using var stream = file.OpenReadStream();
        var url = await _storage.UploadAsync(stream, file.FileName, file.ContentType, "avatars", cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<UploadResultResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "File uploaded successfully",
            Data = new UploadResultResponse { Url = url }
        });
    }

    [HttpPost("receipt")]
    [RequestSizeLimit(MaxImageBytes + 1024)]
    public async Task<IActionResult> UploadReceipt(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            throw new BadRequestException("No file was uploaded.");
        if (file.Length > MaxImageBytes)
            throw new BadRequestException("Receipt image must be 5 MB or smaller.");
        if (!AllowedImageTypes.Contains(file.ContentType))
            throw new BadRequestException("Only JPEG, PNG, WebP or GIF images are allowed.");

        await using var stream = file.OpenReadStream();
        var url = await _storage.UploadAsync(stream, file.FileName, file.ContentType, "receipts", cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<UploadResultResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Receipt uploaded successfully",
            Data = new UploadResultResponse { Url = url }
        });
    }

    public class UploadResultResponse
    {
        public string Url { get; set; } = string.Empty;
    }
}

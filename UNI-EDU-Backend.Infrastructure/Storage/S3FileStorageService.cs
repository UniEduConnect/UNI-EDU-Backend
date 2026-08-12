using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UNI_EDU_Backend.Application.Interfaces;

namespace UNI_EDU_Backend.Infrastructure.Storage;

// AWS S3-backed file storage. Streams the upload straight to the bucket and returns
// a public URL (custom PublicBaseUrl/CDN when configured, otherwise the S3 host).
public class S3FileStorageService : IFileStorageService
{
    private readonly S3Options _o;
    private readonly IAmazonS3 _s3;
    private readonly ILogger<S3FileStorageService> _logger;

    public S3FileStorageService(IOptions<S3Options> options, ILogger<S3FileStorageService> logger)
    {
        _o = options.Value;
        _logger = logger;

        var region = RegionEndpoint.GetBySystemName(
            string.IsNullOrWhiteSpace(_o.Region) ? "ap-southeast-1" : _o.Region);

        // Explicit keys (local dev) when provided; otherwise the default credential
        // chain — env vars / shared profile / ECS task role (preferred in prod).
        _s3 = (!string.IsNullOrWhiteSpace(_o.AccessKey) && !string.IsNullOrWhiteSpace(_o.SecretKey))
            ? new AmazonS3Client(_o.AccessKey, _o.SecretKey, region)
            : new AmazonS3Client(region);
    }

    public async Task<string> UploadAsync(
        Stream content, string fileName, string contentType, string folder, CancellationToken cancellationToken)
    {
        // 1. Try uploading to S3 if BucketName and credentials are configured.
        if (!string.IsNullOrWhiteSpace(_o.BucketName) && !string.IsNullOrWhiteSpace(_o.AccessKey) && !string.IsNullOrWhiteSpace(_o.SecretKey))
        {
            try
            {
                var ext = Path.GetExtension(fileName);
                var key = $"{folder.Trim('/')}/{Guid.NewGuid():N}{ext}".TrimStart('/');

                var request = new PutObjectRequest
                {
                    BucketName = _o.BucketName,
                    Key = key,
                    InputStream = content,
                    ContentType = contentType,
                    DisablePayloadSigning = false,
                };

                await _s3.PutObjectAsync(request, cancellationToken);
                _logger.LogInformation("Uploaded object {Key} to S3 bucket {Bucket}.", key, _o.BucketName);

                var baseUrl = !string.IsNullOrWhiteSpace(_o.PublicBaseUrl)
                    ? _o.PublicBaseUrl!.TrimEnd('/')
                    : $"https://{_o.BucketName}.s3.{_o.Region}.amazonaws.com";

                return $"{baseUrl}/{key}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "S3 upload failed. Falling back to local storage.");
            }
        }
        else
        {
            _logger.LogInformation("S3 bucket or credentials are not fully configured. Using local storage fallback.");
        }

        // 2. Fallback to local wwwroot storage (ideal for local development/testing)
        try
        {
            var ext = Path.GetExtension(fileName);
            var randomName = $"{Guid.NewGuid():N}{ext}";

            // Find physical path of wwwroot dynamically
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var wwwroot = Path.Combine(baseDir, "wwwroot");
            if (!Directory.Exists(wwwroot))
            {
                var candidate = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "wwwroot"));
                if (Directory.Exists(candidate) || File.Exists(Path.Combine(Path.GetDirectoryName(candidate)!, "Program.cs")))
                {
                    wwwroot = candidate;
                }
                else
                {
                    var relativeCandidate = Path.Combine(Directory.GetCurrentDirectory(), "UNI-EDU-Backend.API", "wwwroot");
                    if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "UNI-EDU-Backend.API")))
                    {
                        wwwroot = relativeCandidate;
                    }
                    else
                    {
                        wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    }
                }
            }

            var uploadDir = Path.Combine(wwwroot, "uploads", folder.Trim('/'));
            Directory.CreateDirectory(uploadDir);

            var filePath = Path.Combine(uploadDir, randomName);
            await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await content.CopyToAsync(fileStream, cancellationToken);
            }

            _logger.LogInformation("Uploaded file saved locally to {Path}.", filePath);
            return $"http://localhost:5115/uploads/{folder.Trim('/')}/{randomName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local storage fallback failed.");
            throw new InvalidOperationException("Failed to upload file to both S3 and local storage.", ex);
        }
    }
}

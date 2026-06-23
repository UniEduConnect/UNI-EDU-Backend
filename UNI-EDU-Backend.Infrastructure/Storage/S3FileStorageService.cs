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
        if (string.IsNullOrWhiteSpace(_o.BucketName))
            throw new InvalidOperationException("S3 bucket is not configured (set S3:BucketName).");

        // Random key keeps uploads collision-free and avoids leaking the original filename.
        var ext = Path.GetExtension(fileName);
        var key = $"{folder.Trim('/')}/{Guid.NewGuid():N}{ext}".TrimStart('/');

        var request = new PutObjectRequest
        {
            BucketName = _o.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            // No per-object ACL — modern buckets block ACLs; expose objects via a
            // bucket policy or CloudFront instead.
            DisablePayloadSigning = false,
        };

        await _s3.PutObjectAsync(request, cancellationToken);
        _logger.LogInformation("Uploaded object {Key} to bucket {Bucket}.", key, _o.BucketName);

        var baseUrl = !string.IsNullOrWhiteSpace(_o.PublicBaseUrl)
            ? _o.PublicBaseUrl!.TrimEnd('/')
            : $"https://{_o.BucketName}.s3.{_o.Region}.amazonaws.com";

        return $"{baseUrl}/{key}";
    }
}

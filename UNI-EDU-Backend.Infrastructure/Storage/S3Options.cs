namespace UNI_EDU_Backend.Infrastructure.Storage;

// Bound from the "S3" config section. Put AccessKey/SecretKey in .env as
// S3__AccessKey / S3__SecretKey — or leave them empty in production and let the
// ECS task role supply credentials via the default AWS credential chain.
public class S3Options
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "ap-southeast-1";

    // Optional explicit IAM user credentials (local dev). Empty => default credential chain.
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }

    // Optional CDN / custom domain in front of the bucket. Empty => the S3 virtual-hosted URL.
    public string? PublicBaseUrl { get; set; }
}

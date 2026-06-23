namespace UNI_EDU_Backend.Application.Interfaces;

// Uploads a file's bytes to object storage (S3) and returns its publicly-servable URL.
public interface IFileStorageService
{
    Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken);
}

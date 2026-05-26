namespace UNI_EDU_Backend.Application.DTOs.Classes;

public class MaterialResponse
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>"pdf" | "doc" | "image" | "video" | "link"</summary>
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Size { get; set; }
    public DateTime UploadedAt { get; set; }
}

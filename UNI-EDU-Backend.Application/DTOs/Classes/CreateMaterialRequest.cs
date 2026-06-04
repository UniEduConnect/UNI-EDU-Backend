namespace UNI_EDU_Backend.Application.DTOs.Classes;

public class CreateMaterialRequest
{
    public string Name { get; set; } = string.Empty;

    /// <summary>"pdf" | "doc" | "image" | "video" | "link"</summary>
    public string Type { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    // Optional human-readable size label (e.g. "2.4 MB"); not meaningful for links.
    public string? Size { get; set; }
}

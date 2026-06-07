namespace UNI_EDU_Backend.Application.DTOs.Classes;

public class CreateMaterialRequest
{
    public string Name { get; set; } = string.Empty;
    /// <summary>"pdf" | "doc" | "image" | "video" | "link"</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>URL already uploaded via /uploads/document on the client side.</summary>
    public string Url { get; set; } = string.Empty;
    public string? Size { get; set; }
}

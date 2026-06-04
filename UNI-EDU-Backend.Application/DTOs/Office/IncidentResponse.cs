namespace UNI_EDU_Backend.Application.DTOs.Office;

public class IncidentResponse
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public Guid? SessionId { get; set; }

    public string Reporter { get; set; } = string.Empty;
    public string ReporterRole { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // "low" | "medium" | "high"
    public string Priority { get; set; } = "medium";

    // "pending" | "investigating" | "resolved"
    public string Status { get; set; } = "pending";

    public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

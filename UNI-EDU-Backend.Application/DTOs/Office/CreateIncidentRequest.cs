namespace UNI_EDU_Backend.Application.DTOs.Office;

public class CreateIncidentRequest
{
    public Guid ClassId { get; set; }

    // Optional: the specific session the incident relates to.
    public Guid? SessionId { get; set; }

    public string Description { get; set; } = string.Empty;

    // "low" | "medium" | "high"
    public string Priority { get; set; } = "medium";
}

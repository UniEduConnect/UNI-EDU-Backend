namespace UNI_EDU_Backend.Application.DTOs.Admin;

public class AuditLogResponse
{
    public Guid Id { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

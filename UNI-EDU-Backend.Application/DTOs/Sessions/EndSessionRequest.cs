namespace UNI_EDU_Backend.Application.DTOs.Sessions;

public class EndSessionRequest
{
    public string Content { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Homework { get; set; }
    public List<string>? HomeworkFiles { get; set; }
}

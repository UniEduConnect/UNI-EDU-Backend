namespace UNI_EDU_Backend.Application.DTOs.Trials;

public class CreateTrialRequest
{
    public Guid? SubjectId { get; set; }

    // Requested slot (matches the tutor availability shape).
    public string Day { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;

    public string? Message { get; set; }
}

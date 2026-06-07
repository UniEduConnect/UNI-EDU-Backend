namespace UNI_EDU_Backend.Application.DTOs.Trials;

public class CreateTrialRequest
{
    public Guid TutorId { get; set; }

    // Required only when caller is a Parent (enforced in TrialService, not the validator,
    // because the validator does not see the caller's role).
    public Guid? StudentId { get; set; }

    public Guid SubjectId { get; set; }

    // UTC. Must be strictly in the future.
    public DateTime RequestedAt { get; set; }

    public string? Goals { get; set; }
    public string? CurrentLevel { get; set; }
    public string? Note { get; set; }
}

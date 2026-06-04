namespace UNI_EDU_Backend.Application.DTOs.Profile;

// Tutor self-edit. Null fields are left unchanged; collection fields replace the whole list when provided.
public class UpdateTutorProfileRequest
{
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Location { get; set; }
    public string? School { get; set; }
    public string? Degree { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int? HourlyRate { get; set; }
    public int? YearsExperience { get; set; }
    public string? TeachingStyle { get; set; }
    public string? IntroVideoUrl { get; set; }

    // "tutor" | "teacher"
    public string? TutorType { get; set; }

    public List<string>? Certificates { get; set; }
    public List<string>? Achievements { get; set; }

    // When provided, replaces the tutor's subject set.
    public List<Guid>? SubjectIds { get; set; }
}

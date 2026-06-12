namespace UNI_EDU_Backend.Application.DTOs.Tutors;

public class TutorProfileResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Avatar { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public List<string> Subjects { get; set; } = new();
    public string Bio { get; set; } = default!;
    public string School { get; set; } = default!;
    public string Degree { get; set; } = default!;
    public bool DegreeVerified { get; set; }
    public bool TranscriptVerified { get; set; }
    public string VideoUrl { get; set; } = default!;
    public float Rating { get; set; }
    public int TotalReviews { get; set; }
    public int TotalSessions { get; set; }
    public int TestPassRate { get; set; }
    public int HourlyRate { get; set; }
    public List<AvailabilityDayDto> Availability { get; set; } = new();
    public string JoinDate { get; set; } = default!;
    public string Location { get; set; } = default!;
    public string TeachingStyle { get; set; } = default!;
    public List<string> Achievements { get; set; } = new();
    public List<string> Certificates { get; set; } = new();
    public string Role { get; set; } = default!;
    public string Type { get; set; } = "tutor";   // "tutor" | "teacher" (from Tutor.TutorType)
    public bool IsVerified { get; set; }
    public int? YearsExperience { get; set; }
    public string? CurrentSchool { get; set; }
    public decimal PlatformFeeRate { get; set; }
    public List<TutorReviewResponse> Reviews { get; set; } = new();
}

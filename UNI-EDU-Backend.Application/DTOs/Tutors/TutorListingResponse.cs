namespace UNI_EDU_Backend.Application.DTOs.Tutors;

public class TutorListingResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Avatar { get; set; } = default!;
    public List<string> Subjects { get; set; } = new();
    public float Rating { get; set; }
    public int TotalReviews { get; set; }
    public int TotalSessions { get; set; }
    public int YearsExperience { get; set; }
    public int HourlyRate { get; set; }
    public string Location { get; set; } = default!;
    public bool Verified { get; set; }
    public string Bio { get; set; } = default!;
    public string School { get; set; } = default!;
    public string Degree { get; set; } = default!;
    public string Type { get; set; } = default!;
    public List<AvailableSlotDto>? AvailableSlots { get; set; }
    public List<string>? Certificates { get; set; }
    public string? IntroVideoUrl { get; set; }
    public string? TeachingStyle { get; set; }
    public List<string>? Achievements { get; set; }
}

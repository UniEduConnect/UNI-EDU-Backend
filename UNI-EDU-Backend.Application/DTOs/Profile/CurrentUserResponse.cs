namespace UNI_EDU_Backend.Application.DTOs.Profile;

// Unified "who am I" payload. Exactly one of Tutor/Student/Parent is populated, matching Role.
public class CurrentUserResponse
{
    public Guid Id { get; set; }
    public string Fullname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    // "admin" | "tutor" | "teacher" | "student" | "parent"
    public string Role { get; set; } = string.Empty;

    // "pending" | "approved" | "rejected" | "suspended"
    public string Status { get; set; } = "approved";

    public DateTime CreatedAt { get; set; }

    public MeTutorProfile? Tutor { get; set; }
    public MeStudentProfile? Student { get; set; }
    public MeParentProfile? Parent { get; set; }
}

public class MeTutorProfile
{
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? School { get; set; }
    public string? Location { get; set; }
    public string? Degree { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int? HourlyRate { get; set; }
    public int? YearsExperience { get; set; }
    public string? TeachingStyle { get; set; }
    public string? IntroVideoUrl { get; set; }
    public bool IsVerified { get; set; }
    public float? AverageRating { get; set; }

    // "tutor" | "teacher"
    public string TutorType { get; set; } = "tutor";

    public List<string> Subjects { get; set; } = [];
    public List<string> Certificates { get; set; } = [];
    public List<string> Achievements { get; set; } = [];
}

public class MeStudentProfile
{
    public string? School { get; set; }
    public int Grade { get; set; }
    public Guid? ParentId { get; set; }
}

public class MeParentProfile
{
    public int ChildrenCount { get; set; }
}

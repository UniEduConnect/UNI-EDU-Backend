namespace UNI_EDU_Backend.Application.DTOs.Admin;

public class AdminUserResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    // "admin" | "tutor" | "teacher" | "student" | "parent"
    public string Role { get; set; } = string.Empty;

    // "pending" | "approved" | "rejected" | "suspended"
    public string Status { get; set; } = "approved";

    public string? Avatar { get; set; }
    public string? School { get; set; }
    public string? Bio { get; set; }

    // Role-specific (for the admin edit form). Null when not applicable.
    public int? Grade { get; set; }           // student
    public string? Gender { get; set; }       // tutor/teacher
    public string? Degree { get; set; }       // tutor/teacher
    public string? StudentIdNumber { get; set; } // tutor/teacher

    public DateTime CreatedAt { get; set; }
}

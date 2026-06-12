namespace UNI_EDU_Backend.Application.DTOs.Admin;

// Admin-created account. Role decides which sub-entity row is created.
public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Fullname { get; set; } = string.Empty;
    public string Role { get; set; } = "student"; // student | parent | tutor

    // Student
    public string? School { get; set; }
    public int? Grade { get; set; }

    // Tutor
    public string? Gender { get; set; }
    public string? StudentIdNumber { get; set; }
    public string? Degree { get; set; }
}

public class UpdateUserRequest
{
    public string? Fullname { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }

    // Student
    public string? School { get; set; }
    public int? Grade { get; set; }

    // Tutor / Teacher
    public string? Gender { get; set; }
    public string? Degree { get; set; }
    public string? StudentIdNumber { get; set; }
    public string? Bio { get; set; }
}

namespace UNI_EDU_Backend.Application.DTOs.Profile;

public class StudentProfileResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? School { get; set; }
    public int Grade { get; set; }
    public Guid? ParentId { get; set; }
}

public class UpdateStudentProfileRequest
{
    public string? FullName { get; set; }
    public string? School { get; set; }
    public int? Grade { get; set; }
}

public class ParentProfileResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int ChildrenCount { get; set; }
}

public class UpdateParentProfileRequest
{
    public string? FullName { get; set; }
}

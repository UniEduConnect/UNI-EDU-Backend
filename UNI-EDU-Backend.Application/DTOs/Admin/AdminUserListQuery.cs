namespace UNI_EDU_Backend.Application.DTOs.Admin;

public class AdminUserListQuery
{
    // Case-insensitive match against name or email.
    public string? Search { get; set; }

    // "admin" | "tutor" | "teacher" | "student" | "parent". Omit for all.
    public string? Role { get; set; }

    // "pending" | "approved" | "rejected" | "suspended". Omit for all.
    public string? Status { get; set; }

    // 1-based. Default page size is 10 (see AdminService.PageSize).
    public int Page { get; set; } = 1;
}

namespace UNI_EDU_Backend.Application.DTOs.Office;

public class AttendanceListQuery
{
    // "upcoming" | "pending" | "completed" | "reported". Omit for all.
    public string? Status { get; set; }

    // 1-based. Default page size is 20 (see OfficeService.PageSize).
    public int Page { get; set; } = 1;
}

namespace UNI_EDU_Backend.Application.DTOs.Classes;

public class ClassListQuery
{
    // Optional. One of: searching | active | completed | paused | cancelled (case-insensitive).
    // Omit to return every status.
    public string? Status { get; set; }

    // 1-based. Server page size is fixed at 10 (see ClassService.PageSize).
    public int Page { get; set; } = 1;
}

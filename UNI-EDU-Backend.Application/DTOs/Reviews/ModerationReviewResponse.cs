namespace UNI_EDU_Backend.Application.DTOs.Reviews;

public class ModerationReviewResponse
{
    public int Id { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string TutorName { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool Hidden { get; set; }
}

public class ReviewModerationListQuery
{
    // "visible" | "hidden" | "all". Default all.
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
}

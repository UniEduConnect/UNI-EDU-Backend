namespace UNI_EDU_Backend.Application.DTOs.Exams;

public class ExamListQuery
{
    // Case-insensitive match against exam title.
    public string? Search { get; set; }

    public Guid? SubjectId { get; set; }

    // "draft" | "open" | "closed" | "hidden". Omit for all.
    public string? Status { get; set; }

    // 1-based. Default page size is 10 (see ExamService.PageSize).
    public int Page { get; set; } = 1;
}

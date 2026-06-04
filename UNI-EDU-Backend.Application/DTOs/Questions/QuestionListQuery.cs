namespace UNI_EDU_Backend.Application.DTOs.Questions;

public class QuestionListQuery
{
    // Case-insensitive match against question content.
    public string? Search { get; set; }

    // Filter by subject id.
    public Guid? SubjectId { get; set; }

    // "easy" | "medium" | "hard". Omit for all.
    public string? Difficulty { get; set; }

    // 1-based. Default page size is 20 (see QuestionService.PageSize).
    public int Page { get; set; } = 1;
}

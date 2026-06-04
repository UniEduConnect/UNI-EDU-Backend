namespace UNI_EDU_Backend.Application.DTOs.Exams;

// Full replace of an exam's editable config. Manage attached questions via
// POST /api/exams/{id}/questions instead.
public class UpdateExamRequest
{
    public Guid SubjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string Type { get; set; } = "student-test";
    public string Status { get; set; } = "draft";
    public string Difficulty { get; set; } = "medium";
    public decimal Fee { get; set; }
    public int Year { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int MaxAttemptsPerUser { get; set; } = 1;
    public int ScoreScale { get; set; } = 10;
    public bool AiProctoring { get; set; }
}

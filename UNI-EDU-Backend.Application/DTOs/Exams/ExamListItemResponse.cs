namespace UNI_EDU_Backend.Application.DTOs.Exams;

public class ExamListItemResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public int Duration { get; set; }
    public int QuestionCount { get; set; }
    public decimal Fee { get; set; }
    public int Year { get; set; }

    // "draft" | "open" | "closed" | "hidden"
    public string Status { get; set; } = "draft";

    // "easy" | "medium" | "hard"
    public string Difficulty { get; set; } = "medium";

    // "tutor-test" | "student-test"
    public string Type { get; set; } = "student-test";

    // "ai" | "tutor" | "admin"
    public string CreatedBy { get; set; } = "admin";

    public bool AiProctoring { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int MaxAttemptsPerUser { get; set; }
    public int ScoreScale { get; set; }

    // Number of submissions recorded against this exam.
    public int Attempts { get; set; }
    public DateTime CreatedAt { get; set; }
}

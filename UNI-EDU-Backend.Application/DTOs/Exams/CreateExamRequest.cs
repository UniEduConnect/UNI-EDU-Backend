namespace UNI_EDU_Backend.Application.DTOs.Exams;

public class CreateExamRequest
{
    public Guid SubjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Duration { get; set; } // minutes

    // "tutor-test" | "student-test". Defaults to student-test.
    public string Type { get; set; } = "student-test";

    // "ai" | "tutor" | "admin". Defaults to admin.
    public string CreatedBy { get; set; } = "admin";

    // "draft" | "open" | "closed" | "hidden". Defaults to draft.
    public string Status { get; set; } = "draft";

    // "easy" | "medium" | "hard".
    public string Difficulty { get; set; } = "medium";

    public decimal Fee { get; set; }
    public int Year { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int MaxAttemptsPerUser { get; set; } = 1;
    public int ScoreScale { get; set; } = 10;
    public bool AiProctoring { get; set; }

    // Optional: attach an initial, ordered set of existing questions.
    public List<int>? QuestionIds { get; set; }
}

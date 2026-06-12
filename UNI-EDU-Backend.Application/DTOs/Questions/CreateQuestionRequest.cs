namespace UNI_EDU_Backend.Application.DTOs.Questions;

public class CreateQuestionRequest
{
    public Guid SubjectId { get; set; }
    public string Content { get; set; } = string.Empty;

    // "multiple-choice" | "essay". Defaults to multiple-choice.
    public string Type { get; set; } = "multiple-choice";

    // "easy" | "medium" | "hard".
    public string Difficulty { get; set; } = "medium";

    // Exactly 4 options for multiple-choice questions.
    public List<string> Options { get; set; } = [];

    // Zero-based index into Options (0..3).
    public int CorrectAnswer { get; set; }

    public string? Topic { get; set; }
    public string? Standard { get; set; }
}

namespace UNI_EDU_Backend.Application.DTOs.Questions;

public class QuestionResponse
{
    public int Id { get; set; }
    public Guid SubjectId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    // "multiple-choice" | "essay"
    public string Type { get; set; } = "multiple-choice";

    // "easy" | "medium" | "hard"
    public string Difficulty { get; set; } = "medium";

    // [OptionA, OptionB, OptionC, OptionD]
    public List<string> Options { get; set; } = [];

    // Zero-based index into Options. Null when the answer key is hidden (e.g. while taking an exam).
    public int? CorrectAnswer { get; set; }

    public string? Topic { get; set; }
    public string? Standard { get; set; }
    public DateTime CreatedAt { get; set; }
}

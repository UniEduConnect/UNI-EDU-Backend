namespace UNI_EDU_Backend.Application.DTOs.Questions;

// Full replace of a question's editable fields.
public class UpdateQuestionRequest
{
    public Guid SubjectId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "multiple-choice";
    public string Difficulty { get; set; } = "medium";
    public List<string> Options { get; set; } = [];
    public int CorrectAnswer { get; set; }
    public string? Topic { get; set; }
    public string? Standard { get; set; }
}

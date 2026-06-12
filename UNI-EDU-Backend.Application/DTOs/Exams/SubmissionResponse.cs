namespace UNI_EDU_Backend.Application.DTOs.Exams;

public class SubmissionResponse
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string StudentName { get; set; } = string.Empty;

    // Score on the exam's scale (e.g. out of 10 or 100).
    public float Score { get; set; }
    public int ScoreScale { get; set; }

    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }

    public DateTime SubmissionDate { get; set; }
    public string? AIFeedback { get; set; }
}

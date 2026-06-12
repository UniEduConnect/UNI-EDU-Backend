namespace UNI_EDU_Backend.Application.DTOs.Exams;

public class ExamStatItem
{
    public int ExamId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public double AvgScore { get; set; }

    // Percentage of attempts scoring at least half of the exam's scale.
    public double PassRate { get; set; }
    public int ScoreScale { get; set; }
}

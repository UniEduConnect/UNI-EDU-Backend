namespace UNI_EDU_Backend.Application.DTOs.Parents;

// One month's snapshot for a child (computed from real sessions + exam submissions).
public class MonthlyProgressPoint
{
    public string Month { get; set; } = string.Empty;   // "MM/yyyy"
    public double Gpa { get; set; }                      // avg normalized exam score 0–10
    public double Attendance { get; set; }               // completed / (non-cancelled) sessions * 100
    public int SessionsCompleted { get; set; }
    public double HomeworkRate { get; set; }             // completed sessions w/ homework / completed * 100
}

// Per-subject performance for a child.
public class SubjectPerformance
{
    public string Subject { get; set; } = string.Empty;
    public double Score { get; set; }                    // avg normalized exam score 0–10
    public double Target { get; set; } = 8.0;            // fixed goal
    public double HomeworkRate { get; set; }             // completed sessions w/ homework / total * 100
    public double ParticipationRate { get; set; }        // completed / total sessions * 100
    public string Trend { get; set; } = "flat";          // up | down | flat (this vs last month)
}

public class ChildProgressAnalyticsResponse
{
    public List<MonthlyProgressPoint> MonthlyProgress { get; set; } = new();
    public List<SubjectPerformance> SubjectScores { get; set; } = new();
}

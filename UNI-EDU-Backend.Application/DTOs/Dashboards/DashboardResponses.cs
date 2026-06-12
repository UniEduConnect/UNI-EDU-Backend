namespace UNI_EDU_Backend.Application.DTOs.Dashboards;

public class TutorDashboardResponse
{
    public int ActiveClasses { get; set; }
    public int UpcomingSessions { get; set; }
    public decimal MonthlyEarnings { get; set; }
    public decimal WalletBalance { get; set; }
    public float Rating { get; set; }
    public int TotalReviews { get; set; }
    public int PendingTrials { get; set; }
}

public class StudentDashboardResponse
{
    public int ActiveClasses { get; set; }
    public int UpcomingSessions { get; set; }
    public decimal WalletBalance { get; set; }
    public double AvgScore { get; set; }
    public int ExamsTaken { get; set; }
}

public class ParentDashboardResponse
{
    public int ChildrenCount { get; set; }
    public int ActiveClasses { get; set; }
    public decimal WalletBalance { get; set; }
    public int PendingConfirmations { get; set; }
}

public class FinanceDashboardResponse
{
    public decimal TotalRevenue { get; set; }
    public int PendingWithdrawals { get; set; }
    public decimal TotalEscrow { get; set; }
    public int RefundsPending { get; set; }
}

public class OfficeDashboardResponse
{
    public int PendingAttendance { get; set; }
    public int OpenIncidents { get; set; }
    public int ActiveClasses { get; set; }
}

public class ExamsDashboardResponse
{
    public int TotalExams { get; set; }
    public int TotalQuestions { get; set; }
    public int TotalAttempts { get; set; }
    public double AvgScore { get; set; }
}

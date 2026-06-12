namespace UNI_EDU_Backend.Application.DTOs.Reports;

public class MonthlyAmount
{
    public string Month { get; set; } = string.Empty; // "yyyy-MM"
    public decimal Amount { get; set; }
}

public class MonthlyCount
{
    public string Month { get; set; } = string.Empty; // "yyyy-MM"
    public int Count { get; set; }
}

public class TypeAmount
{
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class AdminReportResponse
{
    public int TotalUsers { get; set; }
    public int TotalClasses { get; set; }
    public int TotalExams { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<MonthlyAmount> MonthlyRevenue { get; set; } = [];
    public List<MonthlyCount> NewUsersByMonth { get; set; } = [];
    public List<TypeAmount> RevenueByType { get; set; } = [];
}

public class FinanceReportResponse
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalWithdrawn { get; set; }
    public decimal TotalRefunded { get; set; }
    public decimal TotalEscrow { get; set; }
    public List<MonthlyAmount> MonthlyRevenue { get; set; } = [];
}

public class OfficeReportResponse
{
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int MissedSessions { get; set; }
    public int OpenIncidents { get; set; }
    public int ResolvedIncidents { get; set; }
}

public class StudentReportResponse
{
    public int ExamsTaken { get; set; }
    public double AvgScore { get; set; }
    public int ActiveClasses { get; set; }
    public int CompletedSessions { get; set; }
}

public class ParentReportResponse
{
    public int ChildrenCount { get; set; }
    public int ActiveClasses { get; set; }
    public decimal TotalSpent { get; set; }
    public double AvgChildScore { get; set; }
}

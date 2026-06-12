namespace UNI_EDU_Backend.Application.DTOs.Admin;

public class DashboardResponse
{
    public int TotalUsers { get; set; }
    public int Tutors { get; set; }
    public int Teachers { get; set; }
    public int Students { get; set; }
    public int Parents { get; set; }
    public int PendingApprovals { get; set; }

    public int TotalClasses { get; set; }
    public int ActiveClasses { get; set; }

    public int TotalExams { get; set; }

    // Gross tuition transacted (sum of completed escrow-in entries), in VND.
    public decimal TotalRevenue { get; set; }

    public int PendingWithdrawals { get; set; }
    public int OpenIncidents { get; set; }
}

using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Reports;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class ReportRepository(ApplicationDbContext dbContext) : IReportRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<AdminReportResponse> GetAdminReportAsync(CancellationToken cancellationToken) => new()
    {
        TotalUsers = await _dbContext.Users.CountAsync(cancellationToken),
        TotalClasses = await _dbContext.Classes.CountAsync(cancellationToken),
        TotalExams = await _dbContext.Exams.CountAsync(cancellationToken),
        TotalRevenue = await RevenueTotalAsync(cancellationToken),
        MonthlyRevenue = await MonthlyRevenueAsync(cancellationToken)
    };

    public async Task<FinanceReportResponse> GetFinanceReportAsync(CancellationToken cancellationToken) => new()
    {
        TotalRevenue = await RevenueTotalAsync(cancellationToken),
        TotalWithdrawn = await _dbContext.WalletTransactions
            .Where(t => t.Type == WalletTxType.Withdrawal && t.Status == WalletTxStatus.Completed)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m,
        TotalRefunded = await _dbContext.WalletTransactions
            .Where(t => t.Type == WalletTxType.Refund && t.Status == WalletTxStatus.Completed)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m,
        TotalEscrow = await _dbContext.Wallets.SumAsync(w => (decimal?)w.EscrowBalance, cancellationToken) ?? 0m,
        MonthlyRevenue = await MonthlyRevenueAsync(cancellationToken)
    };

    public async Task<OfficeReportResponse> GetOfficeReportAsync(CancellationToken cancellationToken) => new()
    {
        TotalSessions = await _dbContext.Sessions.CountAsync(cancellationToken),
        CompletedSessions = await _dbContext.Sessions.CountAsync(s => s.Status == SessionStatus.Completed, cancellationToken),
        MissedSessions = await _dbContext.Sessions.CountAsync(s => s.Status == SessionStatus.Missed, cancellationToken),
        OpenIncidents = await _dbContext.Incidents.CountAsync(i => i.Status != IncidentStatus.Resolved, cancellationToken),
        ResolvedIncidents = await _dbContext.Incidents.CountAsync(i => i.Status == IncidentStatus.Resolved, cancellationToken)
    };

    public async Task<StudentReportResponse> GetStudentReportAsync(Guid studentId, CancellationToken cancellationToken) => new()
    {
        ExamsTaken = await _dbContext.Submissions.CountAsync(s => s.UserID == studentId, cancellationToken),
        AvgScore = await _dbContext.Submissions.Where(s => s.UserID == studentId).Select(s => (double?)s.Score).AverageAsync(cancellationToken) ?? 0d,
        ActiveClasses = await _dbContext.Classes.CountAsync(c => c.StudentID == studentId && c.Status == ClassStatus.Active, cancellationToken),
        CompletedSessions = await _dbContext.Sessions.CountAsync(s => s.Class.StudentID == studentId && s.Status == SessionStatus.Completed, cancellationToken)
    };

    public async Task<ParentReportResponse> GetParentReportAsync(Guid parentId, CancellationToken cancellationToken)
    {
        var childIds = await _dbContext.Students.Where(s => s.ParentID == parentId).Select(s => s.StudentID).ToListAsync(cancellationToken);

        return new ParentReportResponse
        {
            ChildrenCount = childIds.Count,
            ActiveClasses = await _dbContext.Classes.CountAsync(c => childIds.Contains(c.StudentID) && c.Status == ClassStatus.Active, cancellationToken),
            TotalSpent = await _dbContext.WalletTransactions
                .Where(t => t.UserID == parentId && t.Type == WalletTxType.EscrowIn && t.Status == WalletTxStatus.Completed)
                .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m,
            AvgChildScore = await _dbContext.Submissions.Where(s => childIds.Contains(s.UserID)).Select(s => (double?)s.Score).AverageAsync(cancellationToken) ?? 0d
        };
    }

    private async Task<decimal> RevenueTotalAsync(CancellationToken cancellationToken) =>
        await _dbContext.WalletTransactions
            .Where(t => t.Type == WalletTxType.EscrowIn && t.Status == WalletTxStatus.Completed)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

    private async Task<List<MonthlyAmount>> MonthlyRevenueAsync(CancellationToken cancellationToken)
    {
        var rows = await _dbContext.WalletTransactions
            .Where(t => t.Type == WalletTxType.EscrowIn && t.Status == WalletTxStatus.Completed)
            .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(r => r.Year).ThenBy(r => r.Month)
            .TakeLast(6)
            .Select(r => new MonthlyAmount { Month = $"{r.Year:D4}-{r.Month:D2}", Amount = r.Amount })
            .ToList();
    }
}

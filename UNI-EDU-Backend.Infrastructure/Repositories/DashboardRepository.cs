using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Dashboards;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class DashboardRepository(ApplicationDbContext dbContext) : IDashboardRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<TutorDashboardResponse> GetTutorDashboardAsync(Guid tutorId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new TutorDashboardResponse
        {
            ActiveClasses = await _dbContext.Classes.CountAsync(c => c.TutorID == tutorId && c.Status == ClassStatus.Active, cancellationToken),
            UpcomingSessions = await _dbContext.Sessions.CountAsync(s => s.Class.TutorID == tutorId && s.Status == SessionStatus.Scheduled && s.StartAt >= now, cancellationToken),
            MonthlyEarnings = await _dbContext.WalletTransactions
                .Where(t => t.UserID == tutorId && t.Type == WalletTxType.EscrowRelease && t.CreatedAt >= monthStart)
                .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m,
            WalletBalance = await BalanceAsync(tutorId, cancellationToken),
            Rating = await _dbContext.Tutors.Where(t => t.TutorID == tutorId).Select(t => t.AverageRating ?? 0f).FirstOrDefaultAsync(cancellationToken),
            TotalReviews = await _dbContext.Reviews.CountAsync(r => r.TutorID == tutorId, cancellationToken),
            PendingTrials = await _dbContext.TrialBookings.CountAsync(t => t.TutorID == tutorId && t.Status == TrialStatus.Pending, cancellationToken)
        };
    }

    public async Task<StudentDashboardResponse> GetStudentDashboardAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return new StudentDashboardResponse
        {
            ActiveClasses = await _dbContext.Classes.CountAsync(c => c.StudentID == studentId && c.Status == ClassStatus.Active, cancellationToken),
            UpcomingSessions = await _dbContext.Sessions.CountAsync(s => s.Class.StudentID == studentId && s.Status == SessionStatus.Scheduled && s.StartAt >= now, cancellationToken),
            WalletBalance = await BalanceAsync(studentId, cancellationToken),
            AvgScore = await _dbContext.Submissions.Where(s => s.UserID == studentId).Select(s => (double?)s.Score).AverageAsync(cancellationToken) ?? 0d,
            ExamsTaken = await _dbContext.Submissions.CountAsync(s => s.UserID == studentId, cancellationToken)
        };
    }

    public async Task<ParentDashboardResponse> GetParentDashboardAsync(Guid parentId, CancellationToken cancellationToken)
    {
        return new ParentDashboardResponse
        {
            ChildrenCount = await _dbContext.Students.CountAsync(s => s.ParentID == parentId, cancellationToken),
            ActiveClasses = await _dbContext.Classes.CountAsync(c => c.Student.ParentID == parentId && c.Status == ClassStatus.Active, cancellationToken),
            WalletBalance = await BalanceAsync(parentId, cancellationToken),
            PendingConfirmations = await _dbContext.Sessions.CountAsync(s => s.Class.Student.ParentID == parentId && s.Status == SessionStatus.PendingConfirm, cancellationToken)
        };
    }

    public async Task<FinanceDashboardResponse> GetFinanceDashboardAsync(CancellationToken cancellationToken)
    {
        return new FinanceDashboardResponse
        {
            TotalRevenue = await _dbContext.WalletTransactions
                .Where(t => t.Type == WalletTxType.EscrowIn && t.Status == WalletTxStatus.Completed)
                .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m,
            PendingWithdrawals = await _dbContext.Withdrawals.CountAsync(w => w.Status == WithdrawalStatus.Pending, cancellationToken),
            TotalEscrow = await _dbContext.Wallets.SumAsync(w => (decimal?)w.EscrowBalance, cancellationToken) ?? 0m,
            RefundsPending = await _dbContext.RefundRequests.CountAsync(r => r.Status == RefundStatus.Pending, cancellationToken)
        };
    }

    public async Task<OfficeDashboardResponse> GetOfficeDashboardAsync(CancellationToken cancellationToken)
    {
        return new OfficeDashboardResponse
        {
            PendingAttendance = await _dbContext.Sessions.CountAsync(s => s.Status == SessionStatus.PendingConfirm, cancellationToken),
            OpenIncidents = await _dbContext.Incidents.CountAsync(i => i.Status != IncidentStatus.Resolved, cancellationToken),
            ActiveClasses = await _dbContext.Classes.CountAsync(c => c.Status == ClassStatus.Active, cancellationToken)
        };
    }

    public async Task<ExamsDashboardResponse> GetExamsDashboardAsync(CancellationToken cancellationToken)
    {
        return new ExamsDashboardResponse
        {
            TotalExams = await _dbContext.Exams.CountAsync(cancellationToken),
            TotalQuestions = await _dbContext.Questions.CountAsync(cancellationToken),
            TotalAttempts = await _dbContext.Submissions.CountAsync(cancellationToken),
            AvgScore = await _dbContext.Submissions.Select(s => (double?)s.Score).AverageAsync(cancellationToken) ?? 0d
        };
    }

    private async Task<decimal> BalanceAsync(Guid userId, CancellationToken cancellationToken) =>
        await _dbContext.Wallets.Where(w => w.UserID == userId).Select(w => w.Balance).FirstOrDefaultAsync(cancellationToken);
}

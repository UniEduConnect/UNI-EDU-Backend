using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Parents;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class ChildProgressRepository(ApplicationDbContext dbContext) : IChildProgressRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<ChildProgressAnalyticsResponse> GetChildProgressAsync(Guid childId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var windowStart = monthStart.AddMonths(-5); // 6-month window incl. current

        // Real sessions for this child (joined to class → subject).
        var sessions = await _dbContext.Sessions.AsNoTracking()
            .Where(s => s.Class.StudentID == childId)
            .Select(s => new { s.StartAt, s.Status, s.Homework, SubjectId = s.Class.SubjectID, SubjectName = s.Class.Subject.SubjectName })
            .ToListAsync(cancellationToken);

        // Real exam submissions for this child (joined to exam → subject; normalize to 0–10).
        var subs = await _dbContext.Submissions.AsNoTracking()
            .Where(s => s.UserID == childId && s.Exam.ScoreScale > 0)
            .Select(s => new { s.SubmissionDate, Norm = (double)s.Score / s.Exam.ScoreScale * 10.0, SubjectId = s.Exam.SubjectID, SubjectName = s.Exam.Subject.SubjectName })
            .ToListAsync(cancellationToken);

        // Subjects the child is enrolled in (from their classes).
        var enrolled = await _dbContext.Classes.AsNoTracking()
            .Where(c => c.StudentID == childId)
            .Select(c => new { c.SubjectID, Name = c.Subject.SubjectName })
            .Distinct()
            .ToListAsync(cancellationToken);

        // ---- Monthly series (last 6 months) ----
        var monthly = new List<MonthlyProgressPoint>();
        for (var i = 5; i >= 0; i--)
        {
            var m = monthStart.AddMonths(-i);
            var monthSubs = subs.Where(s => s.SubmissionDate.Year == m.Year && s.SubmissionDate.Month == m.Month).ToList();
            var monthSess = sessions.Where(s => s.StartAt.Year == m.Year && s.StartAt.Month == m.Month).ToList();
            var completed = monthSess.Count(s => s.Status == SessionStatus.Completed);
            var counted = monthSess.Count(s => s.Status != SessionStatus.Cancelled);
            var hwDoneMonth = monthSess.Count(s => s.Status == SessionStatus.Completed && !string.IsNullOrWhiteSpace(s.Homework));

            monthly.Add(new MonthlyProgressPoint
            {
                Month = m.ToString("MM/yyyy"),
                Gpa = monthSubs.Count > 0 ? Math.Round(monthSubs.Average(s => s.Norm), 1) : 0,
                Attendance = counted > 0 ? Math.Round((double)completed / counted * 100, 0) : 0,
                SessionsCompleted = completed,
                HomeworkRate = completed > 0 ? Math.Round((double)hwDoneMonth / completed * 100, 0) : 0,
            });
        }

        // ---- Per-subject ----
        var prevMonth = monthStart.AddMonths(-1);
        var subjectScores = new List<SubjectPerformance>();
        foreach (var subj in enrolled)
        {
            var sSubs = subs.Where(x => x.SubjectId == subj.SubjectID).ToList();
            var sSess = sessions.Where(x => x.SubjectId == subj.SubjectID).ToList();
            var total = sSess.Count;
            var completed = sSess.Count(x => x.Status == SessionStatus.Completed);
            var hwDone = sSess.Count(x => x.Status == SessionStatus.Completed && !string.IsNullOrWhiteSpace(x.Homework));

            var thisMonthAvg = sSubs.Where(x => x.SubmissionDate.Year == now.Year && x.SubmissionDate.Month == now.Month).Select(x => x.Norm).DefaultIfEmpty(0).Average();
            var lastMonthAvg = sSubs.Where(x => x.SubmissionDate.Year == prevMonth.Year && x.SubmissionDate.Month == prevMonth.Month).Select(x => x.Norm).DefaultIfEmpty(0).Average();
            var trend = "flat";
            if (thisMonthAvg > 0 && lastMonthAvg > 0)
                trend = thisMonthAvg > lastMonthAvg ? "up" : (thisMonthAvg < lastMonthAvg ? "down" : "flat");

            subjectScores.Add(new SubjectPerformance
            {
                Subject = subj.Name,
                Score = sSubs.Count > 0 ? Math.Round(sSubs.Average(x => x.Norm), 1) : 0,
                Target = 8.0,
                // Homework is only captured on completed sessions → denominator is completed (not total).
                HomeworkRate = completed > 0 ? Math.Round((double)hwDone / completed * 100, 0) : 0,
                ParticipationRate = total > 0 ? Math.Round((double)completed / total * 100, 0) : 0,
                Trend = trend,
            });
        }

        return new ChildProgressAnalyticsResponse { MonthlyProgress = monthly, SubjectScores = subjectScores };
    }
}

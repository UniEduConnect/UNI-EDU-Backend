using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.DTOs.Sessions;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class SessionRepository(ApplicationDbContext dbContext) : ISessionRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public Task<bool> IsParentOfStudentAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken) =>
        _dbContext.Students.AnyAsync(s => s.StudentID == studentId && s.ParentID == parentId, cancellationToken);

    public Task<ClassAccess?> GetClassAccessAsync(Guid classId, CancellationToken cancellationToken) =>
        _dbContext.Classes
            .AsNoTracking()
            .Where(c => c.ClassID == classId)
            .Select(c => new ClassAccess(c.TutorID, c.StudentID))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<List<SessionResponse>> GetByClassIdAsync(Guid classId, CancellationToken cancellationToken)
    {
        // Materialize first — the enriched shape (text[] homework files, enum-to-string status)
        // maps cleanly client-side rather than fighting EF translation.
        var rows = await _dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.ClassID == classId)
            .OrderBy(s => s.StartAt)
            .ToListAsync(cancellationToken);

        return rows.Select(MapToResponse).ToList();
    }

    public Task<SessionAuthContext?> GetContextAsync(Guid sessionId, CancellationToken cancellationToken) =>
        _dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.SessionID == sessionId)
            .Select(s => new SessionAuthContext(
                s.SessionID,
                s.ClassID,
                s.Class.TutorID,
                s.Class.StudentID,
                s.Status,
                s.AbsenceRequestedBy,
                s.AbsenceApproved))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<SessionResponse> StartAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await LoadTrackedAsync(sessionId, cancellationToken);

        session.Status = SessionStatus.InProgress;
        session.StartedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToResponse(session);
    }

    public async Task<SessionResponse> EndAsync(Guid sessionId, EndSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await LoadTrackedAsync(sessionId, cancellationToken);

        session.Status = SessionStatus.PendingConfirm;
        session.EndedAt = DateTime.UtcNow;
        session.Content = request.Content;
        session.Notes = request.Notes;
        session.Homework = request.Homework;
        session.HomeworkFiles = request.HomeworkFiles ?? new();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToResponse(session);
    }

    public async Task<SessionResponse> ConfirmAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var session = await LoadTrackedAsync(sessionId, cancellationToken);
        var classRow = await _dbContext.Classes
            .FirstOrDefaultAsync(c => c.ClassID == session.ClassID, cancellationToken)
            ?? throw new NotFoundException($"Class with id '{session.ClassID}' not found.");

        session.Status = SessionStatus.Completed;
        classRow.CompletedSessions += 1;

        // Per-session proportional escrow release. The final completed session releases whatever
        // remains so rounding never strands money in escrow; never release more than what's held.
        var remaining = classRow.EscrowAmount - classRow.EscrowReleased;
        decimal release;
        if (classRow.CompletedSessions >= classRow.TotalSessions)
            release = remaining;
        else
            release = classRow.TotalSessions > 0
                ? Math.Round(classRow.EscrowAmount / classRow.TotalSessions, 2)
                : 0m;

        if (release > remaining) release = remaining;
        if (release < 0m) release = 0m;

        if (release > 0m)
        {
            classRow.EscrowReleased += release;

            // Draw down the student's held escrow.
            var studentWallet = await _dbContext.Wallets
                .FirstOrDefaultAsync(w => w.UserID == classRow.StudentID, cancellationToken);
            if (studentWallet is not null)
            {
                studentWallet.EscrowBalance -= release;
                studentWallet.UpdatedAt = now;
            }

            // Credit the tutor's spendable balance (create the wallet row if first payout).
            var tutorWallet = await _dbContext.Wallets
                .FirstOrDefaultAsync(w => w.UserID == classRow.TutorID, cancellationToken);
            if (tutorWallet is null)
            {
                tutorWallet = new Wallet { UserID = classRow.TutorID, Balance = 0m, EscrowBalance = 0m, UpdatedAt = now };
                _dbContext.Wallets.Add(tutorWallet);
            }
            tutorWallet.Balance += release;
            tutorWallet.UpdatedAt = now;

            _dbContext.WalletTransactions.Add(new WalletTransaction
            {
                TransactionID = Guid.NewGuid(),
                UserID = classRow.TutorID,
                Type = WalletTxType.EscrowRelease,
                Amount = release,
                RelatedClassID = classRow.ClassID,
                Description = $"Escrow release for session on {session.StartAt:yyyy-MM-dd} ({classRow.Name})",
                CreatedAt = now
            });
        }

        classRow.EscrowStatus = classRow.EscrowReleased >= classRow.EscrowAmount
            ? EscrowStatus.Completed
            : EscrowStatus.InProgress;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return MapToResponse(session);
    }

    public async Task<SessionResponse> RateAsync(Guid sessionId, RateSessionRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var session = await LoadTrackedAsync(sessionId, cancellationToken);
        var classRow = await _dbContext.Classes
            .FirstOrDefaultAsync(c => c.ClassID == session.ClassID, cancellationToken)
            ?? throw new NotFoundException($"Class with id '{session.ClassID}' not found.");

        session.Rating = request.Rating;
        session.RatingComment = request.Comment;

        // Mirror into the tutor's review aggregate.
        _dbContext.Reviews.Add(new Review
        {
            ReviewerID = classRow.StudentID,
            TutorID = classRow.TutorID,
            ClassID = classRow.ClassID,
            Rating = request.Rating,
            Comment = request.Comment ?? string.Empty,
            ReviewDate = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Recompute the tutor's average over all their reviews (now including this one).
        var average = await _dbContext.Reviews
            .Where(r => r.TutorID == classRow.TutorID)
            .AverageAsync(r => (double?)r.Rating, cancellationToken) ?? 0d;

        var tutor = await _dbContext.Tutors
            .FirstOrDefaultAsync(t => t.TutorID == classRow.TutorID, cancellationToken);
        if (tutor is not null)
            tutor.AverageRating = (float)average;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return MapToResponse(session);
    }

    public async Task<SessionResponse> RequestAbsenceAsync(Guid sessionId, CreateAbsenceRequest request, CancellationToken cancellationToken)
    {
        var session = await LoadTrackedAsync(sessionId, cancellationToken);

        session.AbsenceReason = request.Reason;
        session.AbsenceRequestedBy = request.RequestedBy.ToLowerInvariant();
        session.AbsenceApproved = null; // pending counter-party approval

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToResponse(session);
    }

    public async Task<SessionResponse> ApproveAbsenceAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await LoadTrackedAsync(sessionId, cancellationToken);

        session.AbsenceApproved = true;
        session.Status = SessionStatus.Cancelled;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToResponse(session);
    }

    public async Task<SessionResponse> RejectAbsenceAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await LoadTrackedAsync(sessionId, cancellationToken);

        // Denied: the absence is not granted and the session stays as scheduled.
        session.AbsenceApproved = false;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToResponse(session);
    }

    private async Task<Session> LoadTrackedAsync(Guid sessionId, CancellationToken cancellationToken) =>
        await _dbContext.Sessions.FirstOrDefaultAsync(s => s.SessionID == sessionId, cancellationToken)
            ?? throw new NotFoundException($"Session with id '{sessionId}' not found.");

    public async Task<SessionResponse> CreateAsync(Guid classId, DateTime startAt, DateTime endAt, ClassFormat format, CancellationToken cancellationToken)
    {
        // Postgres 'timestamptz' columns require UTC-kind DateTimes. The client sends a plain
        // "yyyy-MM-ddTHH:mm:ss" (no offset) picked from a Vietnam-local <input type="time">, which
        // JSON deserializes as Kind=Unspecified — interpret that as Vietnam time and convert to its
        // true UTC instant (previously this just slapped a Utc label on the Vietnam-local value,
        // storing a time 7 hours off from what the tutor actually picked).
        static DateTime AsUtc(DateTime dt) => dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => SessionScheduling.VietnamToUtc(dt),
        };

        var session = new Session
        {
            SessionID = Guid.NewGuid(),
            ClassID = classId,
            StartAt = AsUtc(startAt),
            EndAt = AsUtc(endAt),
            Status = SessionStatus.Scheduled,
            Format = format,
            CreatedAt = DateTime.UtcNow,
        };

        // Keep the class's planned-session count in sync so progress bars stay correct.
        var classRow = await _dbContext.Classes.FirstOrDefaultAsync(c => c.ClassID == classId, cancellationToken);

        // Reject a manually-added session that clashes with any existing session of this class's
        // tutor or student (double-booking guard).
        if (classRow is not null)
            await Common.ScheduleConflict.EnsureNoConflictAsync(
                _dbContext, classRow.TutorID, classRow.StudentID, new[] { session }, cancellationToken);

        _dbContext.Sessions.Add(session);
        if (classRow is not null) classRow.TotalSessions += 1;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToResponse(session);
    }

    private static SessionResponse MapToResponse(Session s) => new()
    {
        Id = s.SessionID,
        ClassId = s.ClassID,
        StartAt = s.StartAt,
        EndAt = s.EndAt,
        Status = s.Status.ToString().ToLowerInvariant(),
        Format = s.Format.ToString().ToLowerInvariant(),
        StartedAt = s.StartedAt,
        EndedAt = s.EndedAt,
        Content = s.Content,
        Notes = s.Notes,
        Homework = s.Homework,
        HomeworkFiles = s.HomeworkFiles ?? new(),
        Rating = s.Rating,
        RatingComment = s.RatingComment,
        AbsenceReason = s.AbsenceReason,
        AbsenceRequestedBy = s.AbsenceRequestedBy,
        AbsenceApproved = s.AbsenceApproved
    };
}

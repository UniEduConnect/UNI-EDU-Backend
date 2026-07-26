using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Common;

/// <summary>
/// Prevents double-booking. Two guards:
///  - <see cref="EnsureNoConflictAsync"/> — at class/session creation (booking, request/post
///    acceptance, manual add-session): no tutor and no student may end up with overlapping sessions.
///  - <see cref="EnsureNoSelfConflictAsync"/> — at FORM submit time (student posts a "find tutor"
///    request, tutor posts a "find students" ad): the poster's own requested schedule must not clash
///    with their own existing classes, so a conflict is caught immediately, not only once accepted.
/// Both run BEFORE anything is persisted, so a clash aborts the whole operation.
/// </summary>
public static class ScheduleConflict
{
    // Two half-open intervals [aStart, aEnd) and [bStart, bEnd) overlap iff aStart < bEnd && bStart < aEnd.
    // Back-to-back sessions (one ends exactly when the next starts) are NOT a conflict.
    private static bool Overlaps(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd) =>
        aStart < bEnd && bStart < aEnd;

    /// <summary>
    /// Throws <see cref="BadRequestException"/> if any of <paramref name="newSessions"/> overlaps an
    /// existing non-cancelled session belonging to the same tutor or the same student. `excludeClassId`
    /// skips a class's own sessions (used when adding a session to an existing class).
    /// </summary>
    public static async Task EnsureNoConflictAsync(
        ApplicationDbContext db,
        Guid tutorId,
        Guid studentId,
        IReadOnlyCollection<Session> newSessions,
        CancellationToken cancellationToken,
        Guid? excludeClassId = null)
    {
        if (newSessions.Count == 0) return;

        EnsureBatchDoesNotOverlapItself(newSessions);

        var (windowStart, windowEnd) = Window(newSessions);

        // Only pull existing sessions that (a) belong to this tutor or student, (b) sit inside the
        // new sessions' overall time window, and (c) aren't cancelled — the smallest candidate set.
        var existing = await db.Sessions.AsNoTracking()
            .Where(s => s.Status != SessionStatus.Cancelled
                        && s.EndAt > windowStart
                        && s.StartAt < windowEnd
                        && (excludeClassId == null || s.ClassID != excludeClassId)
                        && (s.Class.TutorID == tutorId || s.Class.StudentID == studentId))
            .Select(s => new ExistingSlot(s.StartAt, s.EndAt, s.Class.TutorID, s.Class.StudentID))
            .ToListAsync(cancellationToken);

        foreach (var ns in newSessions)
        {
            foreach (var ex in existing)
            {
                if (!Overlaps(ns.StartAt, ns.EndAt, ex.StartAt, ex.EndAt)) continue;

                var who = ex.TutorId == tutorId ? "Gia sư" : "Học sinh";
                throw new BadRequestException(
                    $"Lịch học bị trùng: {who} đã có buổi học khác vào lúc {ToVietnam(ex.StartAt):HH:mm dd/MM/yyyy}. " +
                    "Vui lòng chọn khung giờ khác.");
            }
        }
    }

    /// <summary>
    /// Form-time guard: throws if the poster's own requested schedule (<paramref name="newSessions"/>)
    /// clashes with itself OR with the poster's existing classes. <paramref name="isTutor"/> selects
    /// whether <paramref name="userId"/> is matched as the tutor or the student on existing classes.
    /// </summary>
    public static async Task EnsureNoSelfConflictAsync(
        ApplicationDbContext db,
        Guid userId,
        bool isTutor,
        IReadOnlyCollection<Session> newSessions,
        CancellationToken cancellationToken)
    {
        if (newSessions.Count == 0) return;

        EnsureBatchDoesNotOverlapItself(newSessions);

        var (windowStart, windowEnd) = Window(newSessions);

        var existing = await db.Sessions.AsNoTracking()
            .Where(s => s.Status != SessionStatus.Cancelled
                        && s.EndAt > windowStart
                        && s.StartAt < windowEnd
                        && (isTutor ? s.Class.TutorID == userId : s.Class.StudentID == userId))
            .Select(s => new { s.StartAt, s.EndAt })
            .ToListAsync(cancellationToken);

        foreach (var ns in newSessions)
        {
            foreach (var ex in existing)
            {
                if (!Overlaps(ns.StartAt, ns.EndAt, ex.StartAt, ex.EndAt)) continue;

                var kind = isTutor ? "lịch dạy" : "lịch học";
                throw new BadRequestException(
                    $"Khung giờ bạn chọn trùng với {kind} hiện có của bạn (buổi lúc {ToVietnam(ex.StartAt):HH:mm dd/MM/yyyy}). " +
                    "Vui lòng chọn khung giờ khác.");
            }
        }
    }

    /// <summary>
    /// Form-time convenience: builds the would-be sessions a request/post schedule (e.g.
    /// "T2 19:00-21:00, T5 18:00-20:00") would generate over its committed duration, then runs
    /// <see cref="EnsureNoSelfConflictAsync"/> against the poster's existing schedule. No-op if the
    /// schedule can't be parsed into any slot.
    /// </summary>
    public static Task EnsureRequestedScheduleFreeAsync(
        ApplicationDbContext db,
        Guid userId,
        bool isTutor,
        string? preferredSchedule,
        int? durationMonths,
        CancellationToken cancellationToken)
    {
        var slots = SessionScheduling.ParsePreferredSchedule(preferredSchedule);
        if (slots.Count == 0) return Task.CompletedTask;

        var now = DateTime.UtcNow;
        var startDate = SessionScheduling.NextWeekMonday(now);
        var total = slots.Count * SessionScheduling.WeeksForDuration(durationMonths);
        var wouldBe = SessionScheduling.BuildPlaceholderSessions(Guid.NewGuid(), ClassFormat.Online, startDate, slots, total, now);

        return EnsureNoSelfConflictAsync(db, userId, isTutor, wouldBe, cancellationToken);
    }

    // A batch must not overlap ITSELF (e.g. two weekly slots on the same day that overlap). Sort by
    // start; a session that begins before the running max end-time of earlier sessions overlaps one.
    private static void EnsureBatchDoesNotOverlapItself(IReadOnlyCollection<Session> newSessions)
    {
        DateTime? runningMaxEnd = null;
        foreach (var s in newSessions.OrderBy(s => s.StartAt))
        {
            if (runningMaxEnd is { } prevEnd && s.StartAt < prevEnd)
                throw new BadRequestException(
                    $"Lịch bạn chọn bị trùng nhau: có hai buổi chồng giờ vào {ToVietnam(s.StartAt):HH:mm dd/MM/yyyy}. " +
                    "Vui lòng chọn các khung giờ không chồng nhau.");
            if (runningMaxEnd is null || s.EndAt > runningMaxEnd) runningMaxEnd = s.EndAt;
        }
    }

    private static (DateTime Start, DateTime End) Window(IReadOnlyCollection<Session> sessions) =>
        (sessions.Min(s => s.StartAt), sessions.Max(s => s.EndAt));

    // Sessions are stored in UTC; show the clash time in Vietnam local so the message is readable.
    private static DateTime ToVietnam(DateTime utc) => utc.AddHours(7);

    private readonly record struct ExistingSlot(DateTime StartAt, DateTime EndAt, Guid TutorId, Guid StudentId);
}

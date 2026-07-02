using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Commons;

// Shared logic for turning a class's WeeklySlots into concrete Session rows, and for
// parsing the free-form "preferred schedule" text students pick when posting a
// "looking for a tutor" request. Used by both the direct class-booking flow
// (ClassRepository) and the class-request acceptance flow (ClassRequestRepository).
public static class SessionScheduling
{
    private static readonly Dictionary<string, DayOfWeek> DayTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CN"] = DayOfWeek.Sunday,
        ["T2"] = DayOfWeek.Monday,
        ["T3"] = DayOfWeek.Tuesday,
        ["T4"] = DayOfWeek.Wednesday,
        ["T5"] = DayOfWeek.Thursday,
        ["T6"] = DayOfWeek.Friday,
        ["T7"] = DayOfWeek.Saturday,
    };

    private static readonly TimeOnly DefaultStart = new(19, 0);
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(90);

    // Parses the schedule text built by the FE's buildSchedule() (PostTutorRequest.tsx),
    // e.g. "T2 19:00-21:00, T5 18:00-20:00" or just "T2, T5" with no time — missing times
    // fall back to a 19:00, 90-minute default slot. Unrecognized segments are skipped rather
    // than throwing, since the request itself is still valid even with malformed schedule text.
    public static List<ClassScheduleSlot> ParsePreferredSchedule(string? preferredSchedule)
    {
        var slots = new List<ClassScheduleSlot>();
        if (string.IsNullOrWhiteSpace(preferredSchedule)) return slots;

        foreach (var rawSegment in preferredSchedule.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = rawSegment.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || !DayTokens.TryGetValue(parts[0], out var dayOfWeek)) continue;

            var start = DefaultStart;
            var end = start.Add(DefaultDuration);

            if (parts.Length > 1)
            {
                var timeParts = parts[1].Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (timeParts.Length > 0 && TimeOnly.TryParse(timeParts[0], out var parsedStart))
                {
                    start = parsedStart;
                    end = start.Add(DefaultDuration);
                }
                if (timeParts.Length > 1 && TimeOnly.TryParse(timeParts[1], out var parsedEnd) && parsedEnd > start)
                {
                    end = parsedEnd;
                }
            }

            slots.Add(new ClassScheduleSlot { DayOfWeek = dayOfWeek, StartTime = start, EndTime = end });
        }

        return slots;
    }

    // Monday of the week AFTER the current one, at UTC midnight — "tuần sau" from whenever
    // the request is accepted, regardless of what day of the week that happens to be.
    public static DateTime NextWeekMonday(DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7; // Monday=0 .. Sunday=6
        var thisWeekMonday = today.AddDays(-daysSinceMonday);
        return thisWeekMonday.AddDays(7).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }

    // ~4.33 weeks/month (52 weeks / 12 months); at least 1 week. Used to size the session
    // count from a class request's DurationMonths commitment.
    public static int WeeksForDuration(int? durationMonths)
    {
        var months = durationMonths is > 0 ? durationMonths.Value : 3;
        return Math.Max(1, (int)Math.Round(months * (52.0 / 12.0)));
    }

    // Walk the calendar forward from startDate. For every day that matches a configured
    // weeklySlots entry, emit one session per slot (ordered by start time) until `total`
    // sessions are placed.
    public static List<Session> BuildPlaceholderSessions(
        Guid classId, ClassFormat format, DateTime startDate, List<ClassScheduleSlot> weeklySlots, int total, DateTime now)
    {
        var sessions = new List<Session>(Math.Max(total, 0));
        if (total <= 0 || weeklySlots.Count == 0) return sessions;

        var slotsByDay = weeklySlots
            .GroupBy(s => s.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.StartTime).ToList());

        var cursor = DateOnly.FromDateTime(startDate);
        // Worst case: 1 session/week -> total weeks. Cap walk at total*7 + 7 days as a safety net.
        var maxDaysToWalk = (total * 7) + 7;

        for (var dayOffset = 0; dayOffset < maxDaysToWalk && sessions.Count < total; dayOffset++)
        {
            var date = cursor.AddDays(dayOffset);
            if (!slotsByDay.TryGetValue(date.DayOfWeek, out var daySlots)) continue;

            foreach (var slot in daySlots)
            {
                if (sessions.Count >= total) break;

                sessions.Add(new Session
                {
                    SessionID = Guid.NewGuid(),
                    ClassID = classId,
                    StartAt = date.ToDateTime(slot.StartTime, DateTimeKind.Utc),
                    EndAt = date.ToDateTime(slot.EndTime, DateTimeKind.Utc),
                    Status = SessionStatus.Scheduled,
                    Format = format,
                    CreatedAt = now
                });
            }
        }

        return sessions;
    }
}

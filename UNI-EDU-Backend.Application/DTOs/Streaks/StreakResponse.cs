namespace UNI_EDU_Backend.Application.DTOs.Streaks;

public class StreakResponse
{
    // Current consecutive-day run. 0 when the streak has lapsed (last activity older than yesterday).
    public int CurrentStreak { get; set; }

    // Best run ever achieved.
    public int LongestStreak { get; set; }

    // Last day (Vietnam time) the user was active, or null if never.
    public DateOnly? LastActivityDate { get; set; }

    // True when the user has already been counted for today's date.
    public bool CheckedInToday { get; set; }
}

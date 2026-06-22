using UNI_EDU_Backend.Application.DTOs.Streaks;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Services.Streaks;

public class StreakService(IGenericRepository<LearningStreak> repo, IUnitOfWork unitOfWork) : IStreakService
{
    private readonly IGenericRepository<LearningStreak> _repo = repo;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    // Vietnam is UTC+7 (no DST) — compute "today" off a fixed offset so the streak
    // rolls over at local midnight instead of 07:00 like a raw UTC date would.
    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

    public async Task<StreakResponse> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var streak = await _repo.GetFirstOrDefaultAsync(x => x.UserID == userId);
        return ToResponse(streak);
    }

    public async Task<StreakResponse> CheckInAsync(Guid userId, CancellationToken cancellationToken)
    {
        var today = Today();
        var streak = await _repo.GetFirstOrDefaultAsync(x => x.UserID == userId);

        if (streak == null)
        {
            streak = new LearningStreak
            {
                UserID = userId,
                CurrentStreak = 1,
                LongestStreak = 1,
                LastActivityDate = today,
                UpdatedAt = DateTime.UtcNow
            };
            await _repo.AddAsync(streak);
        }
        else if (streak.LastActivityDate == today)
        {
            // Already counted today — nothing to persist.
            return ToResponse(streak);
        }
        else
        {
            // Consecutive day advances the run; any gap (or a lapsed streak) restarts at 1.
            streak.CurrentStreak = streak.LastActivityDate == today.AddDays(-1)
                ? streak.CurrentStreak + 1
                : 1;
            if (streak.CurrentStreak > streak.LongestStreak)
                streak.LongestStreak = streak.CurrentStreak;
            streak.LastActivityDate = today;
            streak.UpdatedAt = DateTime.UtcNow;
            _repo.Update(streak);
        }

        await _unitOfWork.SaveChangesAsync();
        return ToResponse(streak);
    }

    private static StreakResponse ToResponse(LearningStreak? streak)
    {
        if (streak == null)
        {
            return new StreakResponse
            {
                CurrentStreak = 0,
                LongestStreak = 0,
                LastActivityDate = null,
                CheckedInToday = false
            };
        }

        var today = Today();
        // The run is alive only if the last activity was today or yesterday; otherwise it
        // has lapsed and reads as 0 until the next check-in revives it. Longest is preserved.
        var alive = streak.LastActivityDate == today || streak.LastActivityDate == today.AddDays(-1);

        return new StreakResponse
        {
            CurrentStreak = alive ? streak.CurrentStreak : 0,
            LongestStreak = streak.LongestStreak,
            LastActivityDate = streak.LastActivityDate,
            CheckedInToday = streak.LastActivityDate == today
        };
    }
}

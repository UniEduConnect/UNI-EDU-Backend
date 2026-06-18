using UNI_EDU_Backend.Application.DTOs.Streaks;

namespace UNI_EDU_Backend.Application.Services.Streaks;

public interface IStreakService
{
    // Read the user's streak without mutating it (lapsed streaks read as 0).
    Task<StreakResponse> GetAsync(Guid userId, CancellationToken cancellationToken);

    // Record activity for today and advance/reset the streak accordingly.
    Task<StreakResponse> CheckInAsync(Guid userId, CancellationToken cancellationToken);
}

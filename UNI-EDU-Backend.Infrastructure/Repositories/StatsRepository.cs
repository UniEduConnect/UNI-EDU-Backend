using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Stats;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class StatsRepository(ApplicationDbContext dbContext) : IStatsRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<PublicStatsResponse> GetPublicStatsAsync(CancellationToken cancellationToken)
    {
        var tutors = await _dbContext.Tutors.AsNoTracking().CountAsync(cancellationToken);
        var students = await _dbContext.Students.AsNoTracking().CountAsync(cancellationToken);
        var classes = await _dbContext.Classes.AsNoTracking().CountAsync(cancellationToken);
        var sessionsCompleted = await _dbContext.Sessions.AsNoTracking()
            .CountAsync(s => s.Status == SessionStatus.Completed, cancellationToken);

        var avgRating = await _dbContext.Reviews.AsNoTracking()
            .Select(r => (double?)r.Rating)
            .AverageAsync(cancellationToken);
        var satisfaction = avgRating is > 0 ? (int)Math.Round(avgRating.Value / 5.0 * 100) : 0;

        return new PublicStatsResponse
        {
            Tutors = tutors,
            Students = students,
            Classes = classes,
            SessionsCompleted = sessionsCompleted,
            SatisfactionPct = satisfaction,
        };
    }
}

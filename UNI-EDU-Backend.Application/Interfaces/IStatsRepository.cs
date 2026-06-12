using UNI_EDU_Backend.Application.DTOs.Stats;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IStatsRepository
{
    Task<PublicStatsResponse> GetPublicStatsAsync(CancellationToken cancellationToken);
}

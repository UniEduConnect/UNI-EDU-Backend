using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Trials;

namespace UNI_EDU_Backend.Application.Services.Trials;

public interface ITrialService
{
    Task<TrialResponse> CreateAsync(Guid tutorId, CreateTrialRequest request, Guid studentId, CancellationToken cancellationToken);
    Task<PagedResult<TrialResponse>> GetMineAsync(TrialListQuery query, Guid studentId, CancellationToken cancellationToken);
    Task<PagedResult<TrialResponse>> GetIncomingAsync(TrialListQuery query, Guid tutorId, CancellationToken cancellationToken);
    Task<TrialResponse> RespondAsync(Guid trialId, Guid tutorId, bool accept, CancellationToken cancellationToken);
}

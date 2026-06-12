using UNI_EDU_Backend.Application.DTOs.AiTests;

namespace UNI_EDU_Backend.Application.Services.AiTests;

public interface IAiTestService
{
    Task<AiTestResponse> GenerateAsync(Guid tutorId, GenerateAiTestRequest request, CancellationToken cancellationToken);
    Task<AiTestResultResponse> SubmitAsync(Guid tutorId, Guid attemptId, SubmitAiTestRequest request, CancellationToken cancellationToken);
}

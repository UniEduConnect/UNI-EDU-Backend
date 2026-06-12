using UNI_EDU_Backend.Application.DTOs.Exams;

namespace UNI_EDU_Backend.Application.Services.Exams;

public interface IAiExamGenerationService
{
    // Generate questions with AI and persist a new exam. Tutor callers own the exam (CreatorTutorID).
    Task<ExamDetailResponse> GenerateAndCreateAsync(GenerateExamWithAiRequest request, Guid callerId, string callerRole, CancellationToken cancellationToken);
}

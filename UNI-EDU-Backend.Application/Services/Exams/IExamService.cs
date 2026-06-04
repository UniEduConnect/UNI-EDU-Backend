using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Exams;

namespace UNI_EDU_Backend.Application.Services.Exams;

public interface IExamService
{
    Task<PagedResult<ExamListItemResponse>> SearchAsync(ExamListQuery query, string callerRole, CancellationToken cancellationToken);
    Task<ExamDetailResponse> GetByIdAsync(int id, string callerRole, CancellationToken cancellationToken);
    Task<ExamDetailResponse> CreateAsync(CreateExamRequest request, CancellationToken cancellationToken);
    Task<ExamDetailResponse> UpdateAsync(int id, UpdateExamRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
    Task<ExamDetailResponse> SetQuestionsAsync(int examId, SetExamQuestionsRequest request, CancellationToken cancellationToken);

    Task<SubmissionResponse> SubmitAsync(int examId, SubmitExamRequest request, Guid userId, CancellationToken cancellationToken);
    Task<PagedResult<SubmissionResponse>> GetExamSubmissionsAsync(int examId, int page, CancellationToken cancellationToken);
    Task<PagedResult<SubmissionResponse>> GetMySubmissionsAsync(Guid userId, int page, CancellationToken cancellationToken);
}

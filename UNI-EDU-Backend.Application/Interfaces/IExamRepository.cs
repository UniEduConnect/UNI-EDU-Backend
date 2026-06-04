using UNI_EDU_Backend.Application.DTOs.Exams;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IExamRepository
{
    Task<bool> SubjectExistsAsync(Guid subjectId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    // includeHidden=false drops draft/hidden exams (student-facing listing).
    Task<(List<ExamListItemResponse> Items, int Total)> SearchAsync(ExamListQuery query, bool includeHidden, int pageSize, CancellationToken cancellationToken);

    // includeAnswerKey=false nulls out each question's CorrectAnswer (exam-taking view).
    Task<ExamDetailResponse?> GetByIdAsync(int id, bool includeAnswerKey, CancellationToken cancellationToken);

    Task<ExamDetailResponse> CreateAsync(CreateExamRequest request, CancellationToken cancellationToken);

    Task<ExamDetailResponse?> UpdateAsync(int id, UpdateExamRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);

    // Returns the subset of ids that do NOT exist (empty when all valid).
    Task<List<int>> GetMissingQuestionIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken);

    // Replaces the exam's full ordered question set.
    Task<ExamDetailResponse?> SetQuestionsAsync(int examId, List<int> questionIds, CancellationToken cancellationToken);

    // Grading
    Task<ExamGradeContext?> GetGradeContextAsync(int examId, CancellationToken cancellationToken);
    Task<int> CountUserAttemptsAsync(int examId, Guid userId, CancellationToken cancellationToken);
    Task<SubmissionResponse> CreateSubmissionAsync(int examId, Guid userId, string answersJson, float score, int correctCount, int totalQuestions, CancellationToken cancellationToken);

    Task<(List<SubmissionResponse> Items, int Total)> GetSubmissionsByExamAsync(int examId, int page, int pageSize, CancellationToken cancellationToken);
    Task<(List<SubmissionResponse> Items, int Total)> GetSubmissionsByUserAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken);
}

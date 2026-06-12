using UNI_EDU_Backend.Application.DTOs.Questions;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IQuestionRepository
{
    Task<bool> SubjectExistsAsync(Guid subjectId, CancellationToken cancellationToken);

    Task<(List<QuestionResponse> Items, int Total)> SearchAsync(QuestionListQuery query, int pageSize, CancellationToken cancellationToken);

    Task<QuestionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<QuestionResponse> CreateAsync(CreateQuestionRequest request, CancellationToken cancellationToken);

    Task<QuestionResponse?> UpdateAsync(int id, UpdateQuestionRequest request, CancellationToken cancellationToken);

    // Returns false when the question does not exist.
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}

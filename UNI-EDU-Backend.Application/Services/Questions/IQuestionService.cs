using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Questions;

namespace UNI_EDU_Backend.Application.Services.Questions;

public interface IQuestionService
{
    Task<PagedResult<QuestionResponse>> SearchAsync(QuestionListQuery query, CancellationToken cancellationToken);
    Task<QuestionResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<QuestionResponse> CreateAsync(CreateQuestionRequest request, CancellationToken cancellationToken);
    Task<QuestionResponse> UpdateAsync(int id, UpdateQuestionRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}

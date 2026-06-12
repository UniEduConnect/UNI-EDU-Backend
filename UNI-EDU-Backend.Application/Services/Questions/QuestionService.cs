using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Questions;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Questions;

public class QuestionService(
    IQuestionRepository questionRepo,
    IValidator<CreateQuestionRequest> createValidator,
    IValidator<UpdateQuestionRequest> updateValidator) : IQuestionService
{
    private const int PageSize = 20;

    private readonly IQuestionRepository _questionRepo = questionRepo;
    private readonly IValidator<CreateQuestionRequest> _createValidator = createValidator;
    private readonly IValidator<UpdateQuestionRequest> _updateValidator = updateValidator;

    public async Task<PagedResult<QuestionResponse>> SearchAsync(QuestionListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var (items, total) = await _questionRepo.SearchAsync(query, PageSize, cancellationToken);

        return new PagedResult<QuestionResponse>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = PageSize
        };
    }

    public async Task<QuestionResponse> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await _questionRepo.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Question with id '{id}' not found.");

    public async Task<QuestionResponse> CreateAsync(CreateQuestionRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        if (!await _questionRepo.SubjectExistsAsync(request.SubjectId, cancellationToken))
            throw new BadRequestException($"Subject with id '{request.SubjectId}' does not exist.");

        return await _questionRepo.CreateAsync(request, cancellationToken);
    }

    public async Task<QuestionResponse> UpdateAsync(int id, UpdateQuestionRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.EnsureValidAsync(request, cancellationToken);

        if (!await _questionRepo.SubjectExistsAsync(request.SubjectId, cancellationToken))
            throw new BadRequestException($"Subject with id '{request.SubjectId}' does not exist.");

        return await _questionRepo.UpdateAsync(id, request, cancellationToken)
            ?? throw new NotFoundException($"Question with id '{id}' not found.");
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        if (!await _questionRepo.DeleteAsync(id, cancellationToken))
            throw new NotFoundException($"Question with id '{id}' not found.");
    }
}

using System.Text.Json;
using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Exams;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Services.Exams;

public class ExamService(
    IExamRepository examRepo,
    IValidator<CreateExamRequest> createValidator,
    IValidator<UpdateExamRequest> updateValidator,
    IValidator<SetExamQuestionsRequest> setQuestionsValidator,
    IValidator<SubmitExamRequest> submitValidator) : IExamService
{
    private const int PageSize = 10;
    private const int SubmissionPageSize = 20;

    private readonly IExamRepository _examRepo = examRepo;
    private readonly IValidator<CreateExamRequest> _createValidator = createValidator;
    private readonly IValidator<UpdateExamRequest> _updateValidator = updateValidator;
    private readonly IValidator<SetExamQuestionsRequest> _setQuestionsValidator = setQuestionsValidator;
    private readonly IValidator<SubmitExamRequest> _submitValidator = submitValidator;

    public async Task<PagedResult<ExamListItemResponse>> SearchAsync(ExamListQuery query, string callerRole, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var includeHidden = IsAdmin(callerRole);

        var (items, total) = await _examRepo.SearchAsync(query, includeHidden, PageSize, cancellationToken);

        return new PagedResult<ExamListItemResponse>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = PageSize
        };
    }

    public async Task<ExamDetailResponse> GetByIdAsync(int id, string callerRole, CancellationToken cancellationToken)
    {
        // Only Admin/managers see the answer key; everyone else gets the take-the-exam view.
        var includeAnswerKey = IsAdmin(callerRole);

        var exam = await _examRepo.GetByIdAsync(id, includeAnswerKey, cancellationToken)
            ?? throw new NotFoundException($"Exam with id '{id}' not found.");

        // Hidden/draft exams are not visible to non-admins.
        if (!includeAnswerKey && exam.Status is "hidden" or "draft")
            throw new NotFoundException($"Exam with id '{id}' not found.");

        return exam;
    }

    public async Task<ExamDetailResponse> CreateAsync(CreateExamRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        if (!await _examRepo.SubjectExistsAsync(request.SubjectId, cancellationToken))
            throw new BadRequestException($"Subject with id '{request.SubjectId}' does not exist.");

        if (request.QuestionIds is { Count: > 0 })
            await EnsureQuestionsExistAsync(request.QuestionIds, cancellationToken);

        return await _examRepo.CreateAsync(request, cancellationToken);
    }

    public async Task<ExamDetailResponse> UpdateAsync(int id, UpdateExamRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.EnsureValidAsync(request, cancellationToken);

        if (!await _examRepo.SubjectExistsAsync(request.SubjectId, cancellationToken))
            throw new BadRequestException($"Subject with id '{request.SubjectId}' does not exist.");

        return await _examRepo.UpdateAsync(id, request, cancellationToken)
            ?? throw new NotFoundException($"Exam with id '{id}' not found.");
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        if (!await _examRepo.DeleteAsync(id, cancellationToken))
            throw new NotFoundException($"Exam with id '{id}' not found.");
    }

    public async Task<ExamDetailResponse> SetQuestionsAsync(int examId, SetExamQuestionsRequest request, CancellationToken cancellationToken)
    {
        await _setQuestionsValidator.EnsureValidAsync(request, cancellationToken);

        if (!await _examRepo.ExistsAsync(examId, cancellationToken))
            throw new NotFoundException($"Exam with id '{examId}' not found.");

        if (request.QuestionIds.Count > 0)
            await EnsureQuestionsExistAsync(request.QuestionIds, cancellationToken);

        return await _examRepo.SetQuestionsAsync(examId, request.QuestionIds, cancellationToken)
            ?? throw new NotFoundException($"Exam with id '{examId}' not found.");
    }

    public async Task<SubmissionResponse> SubmitAsync(int examId, SubmitExamRequest request, Guid userId, CancellationToken cancellationToken)
    {
        await _submitValidator.EnsureValidAsync(request, cancellationToken);

        var ctx = await _examRepo.GetGradeContextAsync(examId, cancellationToken)
            ?? throw new NotFoundException($"Exam with id '{examId}' not found.");

        if (ctx.Status != ExamStatus.Open)
            throw new BadRequestException("This exam is not open for submissions.");

        if (ctx.Questions.Count == 0)
            throw new BadRequestException("This exam has no questions to grade.");

        var attempts = await _examRepo.CountUserAttemptsAsync(examId, userId, cancellationToken);
        if (attempts >= ctx.MaxAttemptsPerUser)
            throw new BadRequestException($"You have reached the maximum of {ctx.MaxAttemptsPerUser} attempt(s) for this exam.");

        // Auto-grade the multiple-choice questions (those with a known correct index).
        var answerByQuestion = request.Answers.ToDictionary(a => a.QuestionId, a => a.SelectedOption);
        var gradable = ctx.Questions.Where(q => q.CorrectIndex is not null).ToList();

        var correctCount = gradable.Count(q =>
            answerByQuestion.TryGetValue(q.QuestionId, out var selected) && selected == q.CorrectIndex);

        var totalGradable = gradable.Count;
        var score = totalGradable > 0
            ? (float)Math.Round((double)correctCount / totalGradable * ctx.ScoreScale, 2)
            : 0f;

        var answersJson = JsonSerializer.Serialize(request.Answers);

        return await _examRepo.CreateSubmissionAsync(
            examId, userId, answersJson, score, correctCount, totalGradable, cancellationToken);
    }

    public async Task<PagedResult<SubmissionResponse>> GetExamSubmissionsAsync(int examId, int page, CancellationToken cancellationToken)
    {
        if (!await _examRepo.ExistsAsync(examId, cancellationToken))
            throw new NotFoundException($"Exam with id '{examId}' not found.");

        page = page < 1 ? 1 : page;
        var (items, total) = await _examRepo.GetSubmissionsByExamAsync(examId, page, SubmissionPageSize, cancellationToken);

        return new PagedResult<SubmissionResponse>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = SubmissionPageSize
        };
    }

    public async Task<PagedResult<SubmissionResponse>> GetMySubmissionsAsync(Guid userId, int page, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        var (items, total) = await _examRepo.GetSubmissionsByUserAsync(userId, page, SubmissionPageSize, cancellationToken);

        return new PagedResult<SubmissionResponse>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = SubmissionPageSize
        };
    }

    public Task<List<ExamStatItem>> GetStatsAsync(CancellationToken cancellationToken) =>
        _examRepo.GetStatsAsync(cancellationToken);

    public Task<ExamAiConfigResponse> GetAiConfigAsync(CancellationToken cancellationToken) =>
        _examRepo.GetAiConfigAsync(cancellationToken);

    public Task<ExamAiConfigResponse> UpdateAiConfigAsync(UpdateExamAiConfigRequest request, CancellationToken cancellationToken) =>
        _examRepo.UpdateAiConfigAsync(request, cancellationToken);

    private async Task EnsureQuestionsExistAsync(List<int> questionIds, CancellationToken cancellationToken)
    {
        var missing = await _examRepo.GetMissingQuestionIdsAsync(questionIds, cancellationToken);
        if (missing.Count > 0)
            throw new BadRequestException($"These question ids do not exist: {string.Join(", ", missing)}.");
    }

    private static bool IsAdmin(string? role) =>
        (role ?? string.Empty).Trim().Equals("Admin", StringComparison.OrdinalIgnoreCase);
}

using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Exams;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Services.Exams;

public class AiExamGenerationService(
    IExamRepository examRepo,
    IAiTestRepository aiTestRepo,
    IAiQuestionGenerator generator,
    IValidator<GenerateExamWithAiRequest> validator) : IAiExamGenerationService
{
    private readonly IExamRepository _examRepo = examRepo;
    private readonly IAiTestRepository _aiTestRepo = aiTestRepo;
    private readonly IAiQuestionGenerator _generator = generator;
    private readonly IValidator<GenerateExamWithAiRequest> _validator = validator;

    public async Task<ExamDetailResponse> GenerateAndCreateAsync(GenerateExamWithAiRequest request, Guid callerId, string callerRole, CancellationToken cancellationToken)
    {
        await _validator.EnsureValidAsync(request, cancellationToken);

        var subjectName = await _aiTestRepo.GetSubjectNameAsync(request.SubjectId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy môn học.");

        var questions = await _generator.GenerateAsync(
            subjectName, request.QuestionCount, request.Topic, request.Grade, request.Difficulty, cancellationToken);

        var difficulty = (request.Difficulty ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "easy" => DifficultyLevel.Easy,
            "hard" => DifficultyLevel.Hard,
            _ => DifficultyLevel.Medium,
        };

        var isTutor = string.Equals((callerRole ?? string.Empty).Trim(), "Tutor", StringComparison.OrdinalIgnoreCase);

        return await _examRepo.CreateAiExamAsync(
            request.SubjectId, request.Title.Trim(), request.Duration, difficulty,
            isTutor ? CreatorType.Tutor : CreatorType.AI,
            isTutor ? callerId : null,
            request.Topic, questions, cancellationToken);
    }
}

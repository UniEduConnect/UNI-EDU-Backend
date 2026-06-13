using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.AiTests;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.AiTests;

public class AiTestService(
    IAiTestRepository repo,
    IAiQuestionGenerator generator,
    IValidator<GenerateAiTestRequest> validator) : IAiTestService
{
    public const int PassThreshold = 80;
    private const int QuestionCount = 5;

    private readonly IAiTestRepository _repo = repo;
    private readonly IAiQuestionGenerator _generator = generator;
    private readonly IValidator<GenerateAiTestRequest> _validator = validator;

    public async Task<AiTestResponse> GenerateAsync(Guid tutorId, GenerateAiTestRequest request, CancellationToken cancellationToken)
    {
        await _validator.EnsureValidAsync(request, cancellationToken);

        var subjectName = await _repo.GetSubjectNameAsync(request.SubjectId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy môn học.");

        // Level the qualification test to the subject AND the class/grade the tutor is applying for.
        var difficulty = DifficultyForGrade(request.Grade);
        var questions = await _generator.GenerateAsync(subjectName, QuestionCount, request.Topic, request.Grade, difficulty, cancellationToken);
        var attemptId = await _repo.CreateAsync(tutorId, request.SubjectId, questions, cancellationToken);

        return new AiTestResponse
        {
            AttemptId = attemptId,
            SubjectId = request.SubjectId,
            Subject = subjectName,
            Grade = request.Grade,
            PassThreshold = PassThreshold,
            Questions = questions.Select((q, i) => new AiTestQuestionDto { Index = i, Content = q.Content, Options = q.Options }).ToList()
        };
    }

    // Higher grades → harder questions; matches the level a tutor of that class must master.
    private static string DifficultyForGrade(int? grade) => grade switch
    {
        >= 10 => "hard",
        >= 6 => "medium",
        >= 1 => "easy",
        _ => "medium"
    };

    public async Task<AiTestResultResponse> SubmitAsync(Guid tutorId, Guid attemptId, SubmitAiTestRequest request, CancellationToken cancellationToken) =>
        await _repo.GradeAsync(attemptId, tutorId, request.Answers, PassThreshold, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy bài test.");
}

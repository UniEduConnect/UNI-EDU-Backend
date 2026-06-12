using UNI_EDU_Backend.Application.DTOs.AiTests;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.AiTests;

public class AiTestService(IAiTestRepository repo, IAiQuestionGenerator generator) : IAiTestService
{
    public const int PassThreshold = 80;
    private const int QuestionCount = 5;

    private readonly IAiTestRepository _repo = repo;
    private readonly IAiQuestionGenerator _generator = generator;

    public async Task<AiTestResponse> GenerateAsync(Guid tutorId, GenerateAiTestRequest request, CancellationToken cancellationToken)
    {
        var subjectName = await _repo.GetSubjectNameAsync(request.SubjectId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy môn học.");

        var questions = await _generator.GenerateAsync(subjectName, QuestionCount, cancellationToken);
        var attemptId = await _repo.CreateAsync(tutorId, request.SubjectId, questions, cancellationToken);

        return new AiTestResponse
        {
            AttemptId = attemptId,
            SubjectId = request.SubjectId,
            Subject = subjectName,
            PassThreshold = PassThreshold,
            Questions = questions.Select((q, i) => new AiTestQuestionDto { Index = i, Content = q.Content, Options = q.Options }).ToList()
        };
    }

    public async Task<AiTestResultResponse> SubmitAsync(Guid tutorId, Guid attemptId, SubmitAiTestRequest request, CancellationToken cancellationToken) =>
        await _repo.GradeAsync(attemptId, tutorId, request.Answers, PassThreshold, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy bài test.");
}

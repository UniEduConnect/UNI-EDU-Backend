using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.AiTests;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class AiTestRepository(ApplicationDbContext dbContext) : IAiTestRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    private sealed record StoredQuestion(string Content, List<string> Options, int CorrectIndex);

    public Task<string?> GetSubjectNameAsync(Guid subjectId, CancellationToken cancellationToken) =>
        _dbContext.Subjects.AsNoTracking()
            .Where(s => s.SubjectID == subjectId)
            .Select(s => (string?)s.SubjectName)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid> CreateAsync(Guid tutorId, Guid subjectId, IReadOnlyList<GeneratedQuestion> questions, CancellationToken cancellationToken)
    {
        var stored = questions.Select(q => new StoredQuestion(q.Content, q.Options, q.CorrectIndex)).ToList();
        var attempt = new AiTestAttempt
        {
            Id = Guid.NewGuid(),
            TutorId = tutorId,
            SubjectId = subjectId,
            QuestionsJson = JsonSerializer.Serialize(stored),
            Passed = false,
            Used = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Set<AiTestAttempt>().Add(attempt);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return attempt.Id;
    }

    public async Task<AiTestResponse?> GetForTakingAsync(Guid attemptId, Guid tutorId, int passThreshold, CancellationToken cancellationToken)
    {
        var row = await _dbContext.Set<AiTestAttempt>().AsNoTracking()
            .Where(a => a.Id == attemptId && a.TutorId == tutorId)
            .Select(a => new { a.Id, a.SubjectId, SubjectName = a.Subject.SubjectName, a.QuestionsJson })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null) return null;

        var stored = JsonSerializer.Deserialize<List<StoredQuestion>>(row.QuestionsJson) ?? new();
        return new AiTestResponse
        {
            AttemptId = row.Id,
            SubjectId = row.SubjectId,
            Subject = row.SubjectName,
            PassThreshold = passThreshold,
            Questions = stored.Select((q, i) => new AiTestQuestionDto { Index = i, Content = q.Content, Options = q.Options }).ToList()
        };
    }

    public async Task<AiTestResultResponse?> GradeAsync(Guid attemptId, Guid tutorId, IReadOnlyList<int> answers, int passThreshold, CancellationToken cancellationToken)
    {
        var attempt = await _dbContext.Set<AiTestAttempt>()
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.TutorId == tutorId, cancellationToken);
        if (attempt is null) return null;

        var stored = JsonSerializer.Deserialize<List<StoredQuestion>>(attempt.QuestionsJson) ?? new();
        var total = stored.Count;
        var correct = 0;
        for (int i = 0; i < total; i++)
        {
            if (i < answers.Count && answers[i] == stored[i].CorrectIndex) correct++;
        }

        var score = total == 0 ? 0 : (int)Math.Round(correct * 100.0 / total);
        attempt.Score = score;
        attempt.Passed = score >= passThreshold;
        attempt.SubmittedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AiTestResultResponse
        {
            AttemptId = attempt.Id,
            Score = score,
            CorrectCount = correct,
            Total = total,
            Passed = attempt.Passed,
            PassThreshold = passThreshold
        };
    }

    public async Task<bool> ConsumeIfPassedAsync(Guid attemptId, Guid tutorId, Guid subjectId, CancellationToken cancellationToken)
    {
        var attempt = await _dbContext.Set<AiTestAttempt>()
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.TutorId == tutorId && a.SubjectId == subjectId, cancellationToken);
        if (attempt is null || !attempt.Passed || attempt.Used) return false;

        attempt.Used = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

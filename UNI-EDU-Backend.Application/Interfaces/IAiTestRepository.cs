using UNI_EDU_Backend.Application.DTOs.AiTests;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IAiTestRepository
{
    Task<string?> GetSubjectNameAsync(Guid subjectId, CancellationToken cancellationToken);

    Task<Guid> CreateAsync(Guid tutorId, Guid subjectId, IReadOnlyList<UNI_EDU_Backend.Application.Interfaces.GeneratedQuestion> questions, CancellationToken cancellationToken);

    // Questions WITHOUT correct answers, for the tutor to take. Null if not found / not theirs.
    Task<AiTestResponse?> GetForTakingAsync(Guid attemptId, Guid tutorId, int passThreshold, CancellationToken cancellationToken);

    // Grades the answers against the stored correct answers, persists, returns the result.
    Task<AiTestResultResponse?> GradeAsync(Guid attemptId, Guid tutorId, IReadOnlyList<int> answers, int passThreshold, CancellationToken cancellationToken);

    // For acceptance: if the attempt is a PASSED, UNUSED test by this tutor for this subject,
    // marks it used and returns true. Otherwise false.
    Task<bool> ConsumeIfPassedAsync(Guid attemptId, Guid tutorId, Guid subjectId, CancellationToken cancellationToken);
}

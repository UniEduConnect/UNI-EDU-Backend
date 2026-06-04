using UNI_EDU_Backend.Application.DTOs.Trials;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface ITrialRepository
{
    Task<bool> TutorExistsAsync(Guid tutorId, CancellationToken cancellationToken);
    Task<bool> SubjectExistsAsync(Guid subjectId, CancellationToken cancellationToken);

    Task<TrialResponse> CreateAsync(Guid studentId, Guid tutorId, CreateTrialRequest request, CancellationToken cancellationToken);

    Task<(List<TrialResponse> Items, int Total)> GetByStudentAsync(Guid studentId, TrialStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<(List<TrialResponse> Items, int Total)> GetByTutorAsync(Guid tutorId, TrialStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    // Tutor accepts/declines. Returns the outcome and (on success) the updated row + the student id to notify.
    Task<(TrialReviewOutcome Outcome, TrialResponse? Trial)> RespondAsync(Guid trialId, Guid tutorId, bool accept, CancellationToken cancellationToken);
}

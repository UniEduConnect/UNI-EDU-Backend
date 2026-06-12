using UNI_EDU_Backend.Application.DTOs.Subjects;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public enum SubjectDeleteOutcome { NotFound, InUse, Deleted }

public interface ISubjectRepository
{
    Task<List<SubjectResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken);
    Task<SubjectResponse> CreateAsync(string name, CancellationToken cancellationToken);
    Task<SubjectResponse?> UpdateAsync(Guid id, string name, CancellationToken cancellationToken);
    Task<SubjectDeleteOutcome> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

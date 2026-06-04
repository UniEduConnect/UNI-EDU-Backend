using UNI_EDU_Backend.Application.DTOs.Subjects;

namespace UNI_EDU_Backend.Application.Services.Subjects;

public interface ISubjectService
{
    Task<List<SubjectResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<SubjectResponse> CreateAsync(SaveSubjectRequest request, CancellationToken cancellationToken);
    Task<SubjectResponse> UpdateAsync(Guid id, SaveSubjectRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

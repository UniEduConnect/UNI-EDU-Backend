using UNI_EDU_Backend.Application.DTOs.Subjects;

namespace UNI_EDU_Backend.Application.Services.Subjects;

public interface ISubjectService
{
    Task<List<SubjectResponse>> GetAllAsync(CancellationToken cancellationToken);
}

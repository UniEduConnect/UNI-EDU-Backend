using UNI_EDU_Backend.Application.DTOs.Subjects;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Services.Subjects;

public class SubjectService(IGenericRepository<Subject> subjectRepo) : ISubjectService
{
    private readonly IGenericRepository<Subject> _subjectRepo = subjectRepo;

    public async Task<List<SubjectResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var subjects = await _subjectRepo.GetAsync(orderBy: q => q.OrderBy(s => s.SubjectName));

        return subjects
            .Select(s => new SubjectResponse { Id = s.SubjectID, Name = s.SubjectName })
            .ToList();
    }
}

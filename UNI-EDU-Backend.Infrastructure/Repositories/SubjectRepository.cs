using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Subjects;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class SubjectRepository(ApplicationDbContext dbContext) : ISubjectRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public Task<List<SubjectResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        _dbContext.Subjects.AsNoTracking()
            .OrderBy(s => s.SubjectName)
            .Select(s => new SubjectResponse { Id = s.SubjectID, Name = s.SubjectName })
            .ToListAsync(cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken) =>
        _dbContext.Subjects.AnyAsync(
            s => s.SubjectName.ToLower() == name.Trim().ToLower() && (excludeId == null || s.SubjectID != excludeId),
            cancellationToken);

    public async Task<SubjectResponse> CreateAsync(string name, CancellationToken cancellationToken)
    {
        var entity = new Subject { SubjectID = Guid.NewGuid(), SubjectName = name.Trim() };
        _dbContext.Subjects.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new SubjectResponse { Id = entity.SubjectID, Name = entity.SubjectName };
    }

    public async Task<SubjectResponse?> UpdateAsync(Guid id, string name, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Subjects.FirstOrDefaultAsync(s => s.SubjectID == id, cancellationToken);
        if (entity is null) return null;

        entity.SubjectName = name.Trim();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new SubjectResponse { Id = entity.SubjectID, Name = entity.SubjectName };
    }

    public async Task<SubjectDeleteOutcome> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Subjects.FirstOrDefaultAsync(s => s.SubjectID == id, cancellationToken);
        if (entity is null) return SubjectDeleteOutcome.NotFound;

        // Guard against deleting a subject still referenced by classes/exams/questions/tutors.
        var inUse = await _dbContext.Classes.AnyAsync(c => c.SubjectID == id, cancellationToken)
                 || await _dbContext.Exams.AnyAsync(e => e.SubjectID == id, cancellationToken)
                 || await _dbContext.Questions.AnyAsync(q => q.SubjectID == id, cancellationToken)
                 || await _dbContext.TutorSubjects.AnyAsync(ts => ts.SubjectID == id, cancellationToken);
        if (inUse) return SubjectDeleteOutcome.InUse;

        _dbContext.Subjects.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return SubjectDeleteOutcome.Deleted;
    }
}

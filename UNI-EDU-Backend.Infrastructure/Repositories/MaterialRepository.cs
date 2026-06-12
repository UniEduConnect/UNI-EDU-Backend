using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class MaterialRepository(ApplicationDbContext dbContext) : IMaterialRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public Task<ClassAccess?> GetClassAccessAsync(Guid classId, CancellationToken cancellationToken) =>
        _dbContext.Classes
            .AsNoTracking()
            .Where(c => c.ClassID == classId)
            .Select(c => new ClassAccess(c.TutorID, c.StudentID))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> IsParentOfStudentAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken) =>
        _dbContext.Students.AnyAsync(s => s.StudentID == studentId && s.ParentID == parentId, cancellationToken);

    public Task<List<MaterialResponse>> GetByClassIdAsync(Guid classId, CancellationToken cancellationToken) =>
        _dbContext.ClassMaterials
            .AsNoTracking()
            .Where(m => m.ClassID == classId)
            .OrderByDescending(m => m.UploadedAt)
            .Select(m => new MaterialResponse
            {
                Id = m.MaterialID,
                ClassId = m.ClassID,
                Name = m.Name,
                Type = m.Type,
                Url = m.Url,
                Size = m.Size,
                UploadedAt = m.UploadedAt
            })
            .ToListAsync(cancellationToken);

    public async Task<MaterialResponse> CreateAsync(Guid classId, CreateMaterialRequest request, CancellationToken cancellationToken)
    {
        var entity = new ClassMaterial
        {
            MaterialID = Guid.NewGuid(),
            ClassID = classId,
            Name = request.Name,
            Type = request.Type.Trim().ToLowerInvariant(),
            Url = request.Url,
            Size = request.Size,
            UploadedAt = DateTime.UtcNow
        };

        _dbContext.ClassMaterials.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MaterialResponse
        {
            Id = entity.MaterialID,
            ClassId = entity.ClassID,
            Name = entity.Name,
            Type = entity.Type,
            Url = entity.Url,
            Size = entity.Size,
            UploadedAt = entity.UploadedAt
        };
    }

    public async Task<bool> DeleteAsync(Guid classId, Guid materialId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ClassMaterials
            .FirstOrDefaultAsync(m => m.MaterialID == materialId && m.ClassID == classId, cancellationToken);
        if (entity is null) return false;

        _dbContext.ClassMaterials.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

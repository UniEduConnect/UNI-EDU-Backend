using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class ClassMaterialRepository(ApplicationDbContext dbContext) : IClassMaterialRepository
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

    public async Task<MaterialResponse> AddAsync(ClassMaterial material, CancellationToken cancellationToken)
    {
        _dbContext.ClassMaterials.Add(material);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MaterialResponse
        {
            Id = material.MaterialID,
            ClassId = material.ClassID,
            Name = material.Name,
            Type = material.Type,
            Url = material.Url,
            Size = material.Size,
            UploadedAt = material.UploadedAt
        };
    }

    public async Task<bool> DeleteAsync(Guid classId, Guid materialId, CancellationToken cancellationToken)
    {
        var material = await _dbContext.ClassMaterials
            .FirstOrDefaultAsync(m => m.MaterialID == materialId && m.ClassID == classId, cancellationToken);

        if (material is null) return false;

        _dbContext.ClassMaterials.Remove(material);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

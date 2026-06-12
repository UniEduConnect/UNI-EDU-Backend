using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Parents;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class ParentChildLinkRepository(ApplicationDbContext dbContext) : IParentChildLinkRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<Guid?> GetStudentUserIdByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLower();
        return await _dbContext.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Student && u.Email != null && u.Email.ToLower() == normalized)
            .Select(u => (Guid?)u.UserID)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> GetCurrentParentOfStudentAsync(Guid studentId, CancellationToken cancellationToken) =>
        await _dbContext.Students.AsNoTracking()
            .Where(s => s.StudentID == studentId)
            .Select(s => s.ParentID)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> HasPendingAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken) =>
        _dbContext.Set<ParentChildLinkRequest>()
            .AnyAsync(r => r.ParentID == parentId && r.StudentID == studentId && r.Status == "pending", cancellationToken);

    public async Task CreateRequestAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken)
    {
        _dbContext.Set<ParentChildLinkRequest>().Add(new ParentChildLinkRequest
        {
            Id = Guid.NewGuid(),
            ParentID = parentId,
            StudentID = studentId,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ParentLinkRequestResponse>> GetPendingForStudentAsync(Guid studentId, CancellationToken cancellationToken) =>
        await _dbContext.Set<ParentChildLinkRequest>().AsNoTracking()
            .Where(r => r.StudentID == studentId && r.Status == "pending")
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ParentLinkRequestResponse
            {
                Id = r.Id,
                ParentName = r.Parent.FullName ?? r.Parent.User.Fullname,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

    public async Task<Guid?> RespondAsync(Guid requestId, Guid studentId, bool approve, CancellationToken cancellationToken)
    {
        var req = await _dbContext.Set<ParentChildLinkRequest>()
            .FirstOrDefaultAsync(r => r.Id == requestId && r.StudentID == studentId && r.Status == "pending", cancellationToken);
        if (req is null) return null;

        req.Status = approve ? "approved" : "rejected";
        req.RespondedAt = DateTime.UtcNow;

        if (approve)
        {
            var student = await _dbContext.Students.FirstOrDefaultAsync(s => s.StudentID == studentId, cancellationToken);
            if (student is not null) student.ParentID = req.ParentID;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return req.ParentID;
    }

    public async Task<string> GetParentNameAsync(Guid parentId, CancellationToken cancellationToken) =>
        await _dbContext.Parents.AsNoTracking()
            .Where(p => p.ParentID == parentId)
            .Select(p => p.FullName ?? p.User.Fullname)
            .FirstOrDefaultAsync(cancellationToken) ?? "Phụ huynh";

    public async Task<string> GetStudentNameAsync(Guid studentId, CancellationToken cancellationToken) =>
        await _dbContext.Students.AsNoTracking()
            .Where(s => s.StudentID == studentId)
            .Select(s => s.FullName ?? s.User.Fullname)
            .FirstOrDefaultAsync(cancellationToken) ?? "Học sinh";
}

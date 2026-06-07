using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class ChatRepository(ApplicationDbContext dbContext) : IChatRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    // --- Class chat ---

    public Task<ClassAccess?> GetClassAccessAsync(Guid classId, CancellationToken cancellationToken) =>
        _dbContext.Classes
            .AsNoTracking()
            .Where(c => c.ClassID == classId)
            .Select(c => new ClassAccess(c.TutorID, c.StudentID))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> IsParentOfStudentAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken) =>
        _dbContext.Students.AnyAsync(s => s.StudentID == studentId && s.ParentID == parentId, cancellationToken);

    public async Task<(List<ChatMessageRow> Items, int Total)> GetClassMessagesAsync(Guid classId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var baseQuery = _dbContext.ChatMessages.AsNoTracking().Where(m => m.ClassID == classId);

        var total = await baseQuery.CountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        // Page from the newest end; reverse so the page reads oldest→newest (newest last).
        var raw = await baseQuery
            .OrderByDescending(m => m.SentAt)
            .ThenByDescending(m => m.MessageID)
            .Skip(skip)
            .Take(pageSize)
            .Select(m => new
            {
                m.MessageID,
                m.ClassID,
                m.SenderID,
                SenderName = m.Sender.Fullname,
                SenderRole = m.Sender.Role,
                m.Message,
                m.SentAt
            })
            .ToListAsync(cancellationToken);

        var items = raw
            .Select(x => new ChatMessageRow(
                x.MessageID, x.ClassID, x.SenderID,
                x.SenderName ?? string.Empty,
                x.SenderRole.ToString().ToLowerInvariant(),
                x.Message, x.SentAt))
            .ToList();
        items.Reverse();

        return (items, total);
    }

    public async Task<ChatMessageRow> AddClassMessageAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        _dbContext.ChatMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var x = await _dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.MessageID == message.MessageID)
            .Select(m => new
            {
                m.MessageID,
                m.ClassID,
                m.SenderID,
                SenderName = m.Sender.Fullname,
                SenderRole = m.Sender.Role,
                m.Message,
                m.SentAt
            })
            .FirstAsync(cancellationToken);

        return new ChatMessageRow(
            x.MessageID, x.ClassID, x.SenderID,
            x.SenderName ?? string.Empty,
            x.SenderRole.ToString().ToLowerInvariant(),
            x.Message, x.SentAt);
    }

    public async Task MarkClassReadAsync(Guid classId, Guid userId, DateTime readAt, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.ClassChatReads
            .FirstOrDefaultAsync(r => r.ClassID == classId && r.UserID == userId, cancellationToken);

        if (existing is null)
            _dbContext.ClassChatReads.Add(new ClassChatRead { ClassID = classId, UserID = userId, LastReadAt = readAt });
        else
            existing.LastReadAt = readAt;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // --- Parent ↔ tutor DM ---

    public Task<bool> TutorExistsAsync(Guid tutorId, CancellationToken cancellationToken) =>
        _dbContext.Tutors.AnyAsync(t => t.TutorID == tutorId, cancellationToken);

    public Task<bool> ParentExistsAsync(Guid parentId, CancellationToken cancellationToken) =>
        _dbContext.Parents.AnyAsync(p => p.ParentID == parentId, cancellationToken);

    public async Task<(List<DmMessage> Items, int Total)> GetDmMessagesAsync(Guid parentId, Guid tutorId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var baseQuery = _dbContext.DmMessages
            .AsNoTracking()
            .Where(d => d.ParentID == parentId && d.TutorID == tutorId);

        var total = await baseQuery.CountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        var items = await baseQuery
            .OrderByDescending(d => d.SentAt)
            .ThenByDescending(d => d.MessageID)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        items.Reverse();
        return (items, total);
    }

    public async Task<DmMessage> AddDmMessageAsync(DmMessage message, CancellationToken cancellationToken)
    {
        _dbContext.DmMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return message;
    }
}

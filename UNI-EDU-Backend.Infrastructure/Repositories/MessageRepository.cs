using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Chat;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class MessageRepository(ApplicationDbContext dbContext) : IMessageRepository
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

    public Task<List<MessageResponse>> GetByClassIdAsync(Guid classId, CancellationToken cancellationToken) =>
        _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.ClassID == classId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageResponse
            {
                Id = m.MessageID,
                ClassId = m.ClassID,
                SenderId = m.SenderID,
                Sender = m.SenderRole,
                SenderName = m.Sender.Fullname,
                Message = m.Content,
                Timestamp = m.CreatedAt,
                Read = m.IsRead
            })
            .ToListAsync(cancellationToken);

    public async Task<MessageResponse> CreateAsync(Guid classId, Guid senderId, string senderRole, string content, CancellationToken cancellationToken)
    {
        var entity = new Message
        {
            MessageID = Guid.NewGuid(),
            ClassID = classId,
            SenderID = senderId,
            SenderRole = senderRole,
            Content = content,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Messages.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var senderName = await _dbContext.Users
            .Where(u => u.UserID == senderId)
            .Select(u => u.Fullname)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return new MessageResponse
        {
            Id = entity.MessageID,
            ClassId = entity.ClassID,
            SenderId = entity.SenderID,
            Sender = entity.SenderRole,
            SenderName = senderName,
            Message = entity.Content,
            Timestamp = entity.CreatedAt,
            Read = entity.IsRead
        };
    }

    public async Task<int> MarkReadAsync(Guid classId, Guid readerId, CancellationToken cancellationToken)
    {
        var unread = await _dbContext.Messages
            .Where(m => m.ClassID == classId && m.SenderID != readerId && !m.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var m in unread)
            m.IsRead = true;

        if (unread.Count > 0)
            await _dbContext.SaveChangesAsync(cancellationToken);

        return unread.Count;
    }

    public async Task<List<ConversationResponse>> GetMyConversationsAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        var trimmed = (role ?? string.Empty).Trim();

        var classesQuery = _dbContext.Classes.AsNoTracking().AsQueryable();
        classesQuery = trimmed switch
        {
            "Tutor" => classesQuery.Where(c => c.TutorID == userId),
            "Student" => classesQuery.Where(c => c.StudentID == userId),
            "Parent" => classesQuery.Where(c => c.Student.ParentID == userId),
            _ => classesQuery // Admin: all
        };

        var classes = await classesQuery
            .Select(c => new
            {
                c.ClassID,
                c.Name,
                TutorName = c.Tutor.FullName ?? c.Tutor.User.Fullname,
                TutorAvatar = c.Tutor.AvatarUrl,
                StudentName = c.Student.FullName ?? c.Student.User.Fullname
            })
            .ToListAsync(cancellationToken);

        var classIds = classes.Select(c => c.ClassID).ToList();

        var messages = await _dbContext.Messages.AsNoTracking()
            .Where(m => classIds.Contains(m.ClassID))
            .Select(m => new { m.ClassID, m.SenderID, m.Content, m.CreatedAt, m.IsRead })
            .ToListAsync(cancellationToken);

        var byClass = messages.GroupBy(m => m.ClassID).ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<ConversationResponse>();
        foreach (var c in classes)
        {
            byClass.TryGetValue(c.ClassID, out var msgs);
            if (msgs is null || msgs.Count == 0) continue; // sidebar shows threads with messages

            var last = msgs.OrderByDescending(m => m.CreatedAt).First();
            var isTutor = trimmed == "Tutor";

            result.Add(new ConversationResponse
            {
                ClassId = c.ClassID,
                ClassName = c.Name ?? string.Empty,
                OtherPartyName = isTutor ? (c.StudentName ?? string.Empty) : (c.TutorName ?? string.Empty),
                OtherPartyAvatar = isTutor ? null : c.TutorAvatar,
                LastMessage = last.Content,
                LastTimestamp = last.CreatedAt,
                UnreadCount = msgs.Count(m => m.SenderID != userId && !m.IsRead)
            });
        }

        return result.OrderByDescending(r => r.LastTimestamp).ToList();
    }
}

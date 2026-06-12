using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

// Caller-agnostic projection of a class chat message (includes denormalized sender display fields).
// The service stamps IsMine relative to the caller.
public record ChatMessageRow(
    Guid Id,
    Guid ClassId,
    Guid SenderId,
    string SenderName,
    string SenderRole,
    string Message,
    DateTime SentAt);

public interface IChatRepository
{
    // --- Class chat ---
    Task<ClassAccess?> GetClassAccessAsync(Guid classId, CancellationToken cancellationToken);
    Task<bool> IsParentOfStudentAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken);

    Task<(List<ChatMessageRow> Items, int Total)> GetClassMessagesAsync(Guid classId, int page, int pageSize, CancellationToken cancellationToken);
    Task<ChatMessageRow> AddClassMessageAsync(ChatMessage message, CancellationToken cancellationToken);
    Task MarkClassReadAsync(Guid classId, Guid userId, DateTime readAt, CancellationToken cancellationToken);

    // --- Parent ↔ tutor DM ---
    Task<bool> TutorExistsAsync(Guid tutorId, CancellationToken cancellationToken);
    Task<bool> ParentExistsAsync(Guid parentId, CancellationToken cancellationToken);

    Task<(List<DmMessage> Items, int Total)> GetDmMessagesAsync(Guid parentId, Guid tutorId, int page, int pageSize, CancellationToken cancellationToken);
    Task<DmMessage> AddDmMessageAsync(DmMessage message, CancellationToken cancellationToken);
}

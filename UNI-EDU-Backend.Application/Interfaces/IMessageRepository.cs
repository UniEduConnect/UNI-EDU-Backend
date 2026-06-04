using UNI_EDU_Backend.Application.DTOs.Chat;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IMessageRepository
{
    Task<ClassAccess?> GetClassAccessAsync(Guid classId, CancellationToken cancellationToken);
    Task<bool> IsParentOfStudentAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken);

    Task<List<MessageResponse>> GetByClassIdAsync(Guid classId, CancellationToken cancellationToken);
    Task<MessageResponse> CreateAsync(Guid classId, Guid senderId, string senderRole, string content, CancellationToken cancellationToken);

    // Marks messages in the thread NOT sent by the reader as read. Returns the count updated.
    Task<int> MarkReadAsync(Guid classId, Guid readerId, CancellationToken cancellationToken);
}

using UNI_EDU_Backend.Application.DTOs.Chat;

namespace UNI_EDU_Backend.Application.Services.Chat;

public interface IChatService
{
    Task<List<MessageResponse>> GetMessagesAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task<MessageResponse> SendMessageAsync(Guid classId, SendMessageRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task<int> MarkReadAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task<List<ConversationResponse>> GetMyConversationsAsync(Guid callerUserId, string callerRole, CancellationToken cancellationToken);
}

using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Chats;

namespace UNI_EDU_Backend.Application.Services.Chats;

public interface IChatService
{
    // --- Class chat (tutor ↔ student ↔ parent) ---
    Task<PagedResult<ChatMessageResponse>> GetClassMessagesAsync(Guid classId, int page, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task<ChatMessageResponse> SendClassMessageAsync(Guid classId, SendMessageRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task MarkClassReadAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken);

    // --- Parent ↔ tutor DM ---
    Task<PagedResult<DmMessageResponse>> GetDmMessagesAsync(Guid contactId, int page, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task<DmMessageResponse> SendDmMessageAsync(Guid contactId, SendMessageRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken);

    // --- Realtime guards (used by the SignalR hub before joining groups / relaying typing) ---
    // Throw NotFound/Forbidden when the caller may not participate.
    Task EnsureClassParticipantAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
    Task EnsureDmAccessAsync(Guid contactId, Guid callerUserId, string callerRole, CancellationToken cancellationToken);
}

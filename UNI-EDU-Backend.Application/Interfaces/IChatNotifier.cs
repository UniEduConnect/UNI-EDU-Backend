using UNI_EDU_Backend.Application.DTOs.Chats;

namespace UNI_EDU_Backend.Application.Interfaces;

// Realtime push abstraction. Implemented in the API layer with SignalR so the Application
// layer stays free of the transport. Services call these after persisting a message.
public interface IChatNotifier
{
    Task NotifyClassMessageAsync(Guid classId, ChatMessageResponse message, CancellationToken cancellationToken);
    Task NotifyDmMessageAsync(Guid recipientUserId, DmMessageResponse message, CancellationToken cancellationToken);
}

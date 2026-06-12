using Microsoft.AspNetCore.SignalR;
using UNI_EDU_Backend.Application.DTOs.Chats;
using UNI_EDU_Backend.Application.Interfaces;

namespace UNI_EDU_Backend.API.Realtime;

// SignalR-backed implementation of the Application-layer IChatNotifier.
public class ChatNotifier(IHubContext<ChatHub> hub) : IChatNotifier
{
    private readonly IHubContext<ChatHub> _hub = hub;

    public Task NotifyClassMessageAsync(Guid classId, ChatMessageResponse message, CancellationToken cancellationToken) =>
        _hub.Clients.Group(ChatHub.ClassGroup(classId)).SendAsync("ReceiveClassMessage", message, cancellationToken);

    public Task NotifyDmMessageAsync(Guid recipientUserId, DmMessageResponse message, CancellationToken cancellationToken) =>
        _hub.Clients.User(recipientUserId.ToString()).SendAsync("ReceiveDmMessage", message, cancellationToken);
}

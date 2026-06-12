using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using UNI_EDU_Backend.Application.Services.Chats;

namespace UNI_EDU_Backend.API.Realtime;

// Bidirectional chat stream. Message broadcasts are pushed from the REST flow via ChatNotifier;
// this hub additionally relays typing indicators and manages class-group membership.
// SignalR maps each connection to Clients.User(...) by the NameIdentifier claim, so DM pushes
// to a recipient's user id reach all of their connections without explicit group joins.
[Authorize]
public class ChatHub(IChatService chatService) : Hub
{
    private readonly IChatService _chatService = chatService;

    public static string ClassGroup(Guid classId) => $"class:{classId}";

    public async Task JoinClass(Guid classId)
    {
        var (userId, role) = Caller();
        await _chatService.EnsureClassParticipantAsync(classId, userId, role, Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, ClassGroup(classId));
    }

    public Task LeaveClass(Guid classId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, ClassGroup(classId));

    public async Task ClassTyping(Guid classId)
    {
        var (userId, role) = Caller();
        await _chatService.EnsureClassParticipantAsync(classId, userId, role, Context.ConnectionAborted);
        await Clients.OthersInGroup(ClassGroup(classId)).SendAsync("ClassTyping", new { classId, userId });
    }

    public async Task DmTyping(Guid contactId)
    {
        var (userId, role) = Caller();
        await _chatService.EnsureDmAccessAsync(contactId, userId, role, Context.ConnectionAborted);
        await Clients.User(contactId.ToString()).SendAsync("DmTyping", new { from = userId });
    }

    private (Guid UserId, string Role) Caller()
    {
        var idClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(idClaim, out var userId))
            throw new HubException("Invalid user identifier claim.");

        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        return (userId, role);
    }
}

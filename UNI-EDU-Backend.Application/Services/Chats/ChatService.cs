using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Chats;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Services.Chats;

public class ChatService(
    IChatRepository chatRepo,
    IChatNotifier notifier,
    IValidator<SendMessageRequest> sendValidator) : IChatService
{
    private const int PageSize = 30;

    private readonly IChatRepository _chatRepo = chatRepo;
    private readonly IChatNotifier _notifier = notifier;
    private readonly IValidator<SendMessageRequest> _sendValidator = sendValidator;

    // --- Class chat ---

    public async Task<PagedResult<ChatMessageResponse>> GetClassMessagesAsync(Guid classId, int page, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        await EnsureClassParticipantAsync(classId, callerUserId, callerRole, cancellationToken);

        if (page < 1) page = 1;
        var (items, total) = await _chatRepo.GetClassMessagesAsync(classId, page, PageSize, cancellationToken);

        return new PagedResult<ChatMessageResponse>
        {
            Items = items.Select(r => MapClass(r, callerUserId)).ToList(),
            Total = total,
            Page = page,
            PageSize = PageSize
        };
    }

    public async Task<ChatMessageResponse> SendClassMessageAsync(Guid classId, SendMessageRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        await EnsureClassParticipantAsync(classId, callerUserId, callerRole, cancellationToken);
        await _sendValidator.EnsureValidAsync(request, cancellationToken);

        var entity = new ChatMessage
        {
            MessageID = Guid.NewGuid(),
            ClassID = classId,
            SenderID = callerUserId,
            Message = request.Message.Trim(),
            SentAt = DateTime.UtcNow
        };

        var row = await _chatRepo.AddClassMessageAsync(entity, cancellationToken);
        var response = MapClass(row, callerUserId);

        await _notifier.NotifyClassMessageAsync(classId, response, cancellationToken);
        return response;
    }

    public async Task MarkClassReadAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        await EnsureClassParticipantAsync(classId, callerUserId, callerRole, cancellationToken);
        await _chatRepo.MarkClassReadAsync(classId, callerUserId, DateTime.UtcNow, cancellationToken);
    }

    // --- Parent ↔ tutor DM ---

    public async Task<PagedResult<DmMessageResponse>> GetDmMessagesAsync(Guid contactId, int page, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var (parentId, tutorId) = await ResolveDmPairAsync(contactId, callerUserId, callerRole, cancellationToken);

        if (page < 1) page = 1;
        var (items, total) = await _chatRepo.GetDmMessagesAsync(parentId, tutorId, page, PageSize, cancellationToken);

        return new PagedResult<DmMessageResponse>
        {
            Items = items.Select(d => MapDm(d, contactId, callerUserId)).ToList(),
            Total = total,
            Page = page,
            PageSize = PageSize
        };
    }

    public async Task<DmMessageResponse> SendDmMessageAsync(Guid contactId, SendMessageRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var (parentId, tutorId) = await ResolveDmPairAsync(contactId, callerUserId, callerRole, cancellationToken);
        await _sendValidator.EnsureValidAsync(request, cancellationToken);

        var entity = new DmMessage
        {
            MessageID = Guid.NewGuid(),
            ParentID = parentId,
            TutorID = tutorId,
            SenderID = callerUserId,
            Message = request.Message.Trim(),
            SentAt = DateTime.UtcNow
        };

        var saved = await _chatRepo.AddDmMessageAsync(entity, cancellationToken);
        var response = MapDm(saved, contactId, callerUserId);

        // Notify the other party (contactId is the counterpart from the caller's perspective).
        await _notifier.NotifyDmMessageAsync(contactId, response, cancellationToken);
        return response;
    }

    // --- Authorization helpers (also exposed for the realtime hub) ---

    public async Task EnsureDmAccessAsync(Guid contactId, Guid callerUserId, string callerRole, CancellationToken cancellationToken) =>
        await ResolveDmPairAsync(contactId, callerUserId, callerRole, cancellationToken);

    public async Task EnsureClassParticipantAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var access = await _chatRepo.GetClassAccessAsync(classId, cancellationToken)
            ?? throw new NotFoundException($"Class with id '{classId}' not found.");

        var role = (callerRole ?? string.Empty).Trim();
        bool allowed = role switch
        {
            "Admin" => true,
            "Tutor" => access.TutorId == callerUserId,
            "Student" => access.StudentId == callerUserId,
            "Parent" => await _chatRepo.IsParentOfStudentAsync(callerUserId, access.StudentId, cancellationToken),
            _ => false
        };

        if (!allowed)
            throw new ForbiddenAccessException("You do not have access to this class chat.");
    }

    // Resolves the (parent, tutor) conversation pair from the caller and the contact.
    // Parent caller: contactId is the tutor. Tutor caller: contactId is the parent.
    private async Task<(Guid ParentId, Guid TutorId)> ResolveDmPairAsync(Guid contactId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        var role = (callerRole ?? string.Empty).Trim();

        if (role == "Parent")
        {
            if (!await _chatRepo.TutorExistsAsync(contactId, cancellationToken))
                throw new NotFoundException($"Tutor with id '{contactId}' not found.");
            return (callerUserId, contactId);
        }

        if (role == "Tutor")
        {
            if (!await _chatRepo.ParentExistsAsync(contactId, cancellationToken))
                throw new NotFoundException($"Parent with id '{contactId}' not found.");
            return (contactId, callerUserId);
        }

        throw new ForbiddenAccessException("Only a parent or tutor can use direct messages.");
    }

    // --- Mapping ---

    private static ChatMessageResponse MapClass(ChatMessageRow r, Guid callerUserId) => new()
    {
        Id = r.Id,
        ClassId = r.ClassId,
        SenderId = r.SenderId,
        SenderName = r.SenderName,
        SenderRole = r.SenderRole,
        Message = r.Message,
        SentAt = r.SentAt,
        IsMine = r.SenderId == callerUserId
    };

    private static DmMessageResponse MapDm(DmMessage d, Guid contactId, Guid callerUserId) => new()
    {
        Id = d.MessageID,
        ContactId = contactId,
        SenderId = d.SenderID,
        Message = d.Message,
        SentAt = d.SentAt,
        IsMine = d.SenderID == callerUserId
    };
}

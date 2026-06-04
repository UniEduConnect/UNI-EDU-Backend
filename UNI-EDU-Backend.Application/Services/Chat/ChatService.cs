using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Chat;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Chat;

public class ChatService(
    IMessageRepository messageRepo,
    IValidator<SendMessageRequest> sendValidator) : IChatService
{
    private readonly IMessageRepository _messageRepo = messageRepo;
    private readonly IValidator<SendMessageRequest> _sendValidator = sendValidator;

    public async Task<List<MessageResponse>> GetMessagesAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        await EnsureParticipantAsync(classId, callerUserId, callerRole, allowAdmin: true, cancellationToken);
        return await _messageRepo.GetByClassIdAsync(classId, cancellationToken);
    }

    public async Task<MessageResponse> SendMessageAsync(Guid classId, SendMessageRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        await _sendValidator.EnsureValidAsync(request, cancellationToken);

        // Sending is restricted to actual thread participants (Admin can read but not post).
        await EnsureParticipantAsync(classId, callerUserId, callerRole, allowAdmin: false, cancellationToken);

        var senderRole = (callerRole ?? string.Empty).Trim().ToLowerInvariant();
        return await _messageRepo.CreateAsync(classId, callerUserId, senderRole, request.Message.Trim(), cancellationToken);
    }

    public async Task<int> MarkReadAsync(Guid classId, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        await EnsureParticipantAsync(classId, callerUserId, callerRole, allowAdmin: false, cancellationToken);
        return await _messageRepo.MarkReadAsync(classId, callerUserId, cancellationToken);
    }

    private async Task EnsureParticipantAsync(Guid classId, Guid callerUserId, string callerRole, bool allowAdmin, CancellationToken cancellationToken)
    {
        var access = await _messageRepo.GetClassAccessAsync(classId, cancellationToken)
            ?? throw new NotFoundException($"Class with id '{classId}' not found.");

        var role = (callerRole ?? string.Empty).Trim();
        bool allowed = role switch
        {
            "Admin" => allowAdmin,
            "Tutor" => access.TutorId == callerUserId,
            "Student" => access.StudentId == callerUserId,
            "Parent" => await _messageRepo.IsParentOfStudentAsync(callerUserId, access.StudentId, cancellationToken),
            _ => false
        };

        if (!allowed)
            throw new ForbiddenAccessException("You do not have access to this class conversation.");
    }
}

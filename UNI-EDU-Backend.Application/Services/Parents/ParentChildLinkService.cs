using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Parents;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Parents;

public class ParentChildLinkService(
    IParentChildLinkRepository repo,
    INotificationRepository notificationRepo,
    IValidator<LinkChildRequest> validator) : IParentChildLinkService
{
    private readonly IParentChildLinkRepository _repo = repo;
    private readonly INotificationRepository _notificationRepo = notificationRepo;
    private readonly IValidator<LinkChildRequest> _validator = validator;

    public async Task RequestLinkAsync(Guid parentId, LinkChildRequest request, CancellationToken cancellationToken)
    {
        await _validator.EnsureValidAsync(request, cancellationToken);
        var email = request.ChildEmail.Trim();

        var studentId = await _repo.GetStudentUserIdByEmailAsync(email, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy học sinh với email '{email}'.");

        if (studentId == parentId)
            throw new BadRequestException("Không thể tự liên kết với chính mình.");

        var currentParent = await _repo.GetCurrentParentOfStudentAsync(studentId, cancellationToken);
        if (currentParent is Guid cp)
        {
            throw new BadRequestException(cp == parentId
                ? "Bạn đã liên kết với học sinh này."
                : "Học sinh này đã được liên kết với một phụ huynh khác.");
        }

        if (await _repo.HasPendingAsync(parentId, studentId, cancellationToken))
            throw new BadRequestException("Bạn đã gửi yêu cầu liên kết, đang chờ học sinh xác nhận.");

        await _repo.CreateRequestAsync(parentId, studentId, cancellationToken);

        var parentName = await _repo.GetParentNameAsync(parentId, cancellationToken);
        await _notificationRepo.CreateAsync(
            studentId,
            "Yêu cầu liên kết phụ huynh",
            $"{parentName} muốn liên kết tài khoản để theo dõi và hỗ trợ học tập của bạn. Vào hồ sơ để xác nhận.",
            "info",
            "/student",
            cancellationToken);
    }

    public Task<List<ParentLinkRequestResponse>> GetPendingForStudentAsync(Guid studentId, CancellationToken cancellationToken) =>
        _repo.GetPendingForStudentAsync(studentId, cancellationToken);

    public async Task RespondAsync(Guid studentId, Guid requestId, bool approve, CancellationToken cancellationToken)
    {
        var parentId = await _repo.RespondAsync(requestId, studentId, approve, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy yêu cầu liên kết.");

        var studentName = await _repo.GetStudentNameAsync(studentId, cancellationToken);
        await _notificationRepo.CreateAsync(
            parentId,
            approve ? "Liên kết được chấp nhận" : "Liên kết bị từ chối",
            approve
                ? $"{studentName} đã chấp nhận liên kết. Bạn có thể theo dõi và thanh toán học phí cho con."
                : $"{studentName} đã từ chối yêu cầu liên kết.",
            approve ? "success" : "warning",
            "/parent/children",
            cancellationToken);
    }
}

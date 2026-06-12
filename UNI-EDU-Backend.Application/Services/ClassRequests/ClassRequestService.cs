using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.ClassRequests;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.ClassRequests;

public class ClassRequestService(
    IClassRequestRepository repo,
    IClassRepository classRepo,
    IAiTestRepository aiTestRepo,
    INotificationRepository notificationRepo,
    IValidator<CreateClassRequestRequest> createValidator) : IClassRequestService
{
    private const int PageSize = 12;

    private readonly IClassRequestRepository _repo = repo;
    private readonly IClassRepository _classRepo = classRepo;
    private readonly IAiTestRepository _aiTestRepo = aiTestRepo;
    private readonly INotificationRepository _notificationRepo = notificationRepo;
    private readonly IValidator<CreateClassRequestRequest> _createValidator = createValidator;

    public async Task CreateAsync(Guid studentId, CreateClassRequestRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);
        await _repo.CreateAsync(studentId, request, cancellationToken);
    }

    public async Task CreateForParentAsync(Guid parentId, CreateClassRequestRequest request, CancellationToken cancellationToken)
    {
        if (request.StudentId is not Guid childId || childId == Guid.Empty)
            throw new BadRequestException("Vui lòng chọn học sinh (con) để đăng bài tìm gia sư.");

        if (!await _classRepo.IsParentOfStudentAsync(parentId, childId, cancellationToken))
            throw new ForbiddenAccessException("Bạn không phải phụ huynh của học sinh này.");

        await _createValidator.EnsureValidAsync(request, cancellationToken);
        await _repo.CreateAsync(childId, request, cancellationToken);
    }

    public Task<PagedResult<ClassRequestResponse>> GetOpenAsync(ClassRequestListQuery query, CancellationToken cancellationToken) =>
        _repo.GetOpenAsync(query, PageSize, cancellationToken);

    public Task<List<ClassRequestResponse>> GetMineAsync(Guid studentId, CancellationToken cancellationToken) =>
        _repo.GetMineAsync(studentId, cancellationToken);

    public async Task AcceptAsync(Guid tutorId, Guid requestId, AcceptClassRequestRequest request, CancellationToken cancellationToken)
    {
        var info = await _repo.GetAcceptInfoAsync(requestId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy yêu cầu tìm gia sư.");

        if (info.Status != "open")
            throw new BadRequestException("Yêu cầu này đã được nhận hoặc đã đóng.");

        // Per-class AI-test gate: the tutor must pass a fresh AI test (>=80%) for THIS subject.
        if (!await _aiTestRepo.ConsumeIfPassedAsync(request.AiTestAttemptId, tutorId, info.SubjectId, cancellationToken))
            throw new BadRequestException("Bạn cần làm và ĐẠT bài test AI (≥80%) cho đúng môn của lớp này trước khi nhận. Mỗi lần nhận lớp cần một bài test riêng.");

        await _repo.AssignAsync(requestId, tutorId, cancellationToken);

        await _notificationRepo.CreateAsync(
            info.StudentId,
            "Gia sư đã nhận lớp",
            "Một gia sư đã nhận yêu cầu tìm gia sư của bạn. Hệ thống sẽ kết nối hai bên.",
            "success",
            "/student",
            cancellationToken);
    }
}

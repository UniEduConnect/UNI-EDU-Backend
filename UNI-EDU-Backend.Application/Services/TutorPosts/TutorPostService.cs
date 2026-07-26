using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.TutorPosts;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.TutorPosts;

public class TutorPostService(
    ITutorPostRepository repo,
    IAiTestRepository aiTestRepo,
    INotificationRepository notificationRepo,
    IValidator<CreateTutorPostRequest> createValidator) : ITutorPostService
{
    private const int PageSize = 12;

    private readonly ITutorPostRepository _repo = repo;
    private readonly IAiTestRepository _aiTestRepo = aiTestRepo;
    private readonly INotificationRepository _notificationRepo = notificationRepo;
    private readonly IValidator<CreateTutorPostRequest> _createValidator = createValidator;

    public async Task CreateAsync(Guid tutorId, CreateTutorPostRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);
        await _repo.CreateAsync(tutorId, request, cancellationToken);
    }

    public Task<PagedResult<TutorPostResponse>> GetOpenAsync(TutorPostListQuery query, Guid callerId, CancellationToken cancellationToken) =>
        _repo.GetOpenAsync(query, PageSize, callerId, cancellationToken);

    public Task<List<TutorPostResponse>> GetMineAsync(Guid tutorId, CancellationToken cancellationToken) =>
        _repo.GetMineAsync(tutorId, cancellationToken);

    public async Task CloseAsync(Guid tutorId, Guid postId, CancellationToken cancellationToken)
    {
        if (!await _repo.CloseAsync(postId, tutorId, cancellationToken))
            throw new NotFoundException("Không tìm thấy tin đăng.");
    }

    public async Task ApplyAsync(Guid studentId, Guid postId, CancellationToken cancellationToken)
    {
        var post = await _repo.GetOpenPostForApplyAsync(postId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy tin tuyển hoặc tin đã đóng.");

        if (post.TutorId == studentId)
            throw new BadRequestException("Không thể tự đăng ký tin của chính mình.");

        if (await _repo.HasPendingApplicationAsync(postId, studentId, cancellationToken))
            throw new BadRequestException("Bạn đã đăng ký tin này, đang chờ gia sư xử lý.");

        await _repo.CreateApplicationAsync(postId, studentId, post.TutorId, post.SubjectId, cancellationToken);

        var studentName = await _repo.GetStudentNameAsync(studentId, cancellationToken);
        var subjectName = await _aiTestRepo.GetSubjectNameAsync(post.SubjectId, cancellationToken) ?? "môn học";
        await _notificationRepo.CreateAsync(
            post.TutorId,
            "Học sinh đăng ký học",
            $"{studentName} đã đăng ký học môn {subjectName}. Hãy làm bài test AI (≥80%) cho môn này để nhận.",
            "info",
            "/tutor/find-students",
            cancellationToken);
    }

    public Task<List<TutorPostApplicationResponse>> GetApplicationsAsync(Guid tutorId, CancellationToken cancellationToken) =>
        _repo.GetApplicationsForTutorAsync(tutorId, cancellationToken);

    public Task EnsureApplicationAcceptableAsync(Guid tutorId, Guid appId, CancellationToken cancellationToken) =>
        _repo.EnsureApplicationAcceptableAsync(tutorId, appId, cancellationToken);

    public async Task AcceptApplicationAsync(Guid tutorId, Guid appId, AcceptApplicationRequest request, CancellationToken cancellationToken)
    {
        var app = await _repo.GetApplicationForAcceptAsync(appId, tutorId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy đăng ký.");

        if (app.Status != "pending")
            throw new BadRequestException("Đăng ký này đã được xử lý.");

        if (app.PostStatus != "open")
            throw new BadRequestException("Tin đăng này đã đóng — không thể nhận thêm học sinh.");

        if (!await _aiTestRepo.ConsumeIfPassedAsync(request.AiTestAttemptId, tutorId, app.SubjectId, cancellationToken))
            throw new BadRequestException("Bạn cần làm và ĐẠT bài test AI (≥80%) cho đúng môn trước khi nhận học sinh.");

        await _repo.AcceptApplicationAsync(appId, cancellationToken);

        await _notificationRepo.CreateAsync(
            app.StudentId,
            "Gia sư đã nhận bạn",
            "Gia sư đã chấp nhận đăng ký học của bạn. Hệ thống sẽ kết nối hai bên.",
            "success",
            "/student",
            cancellationToken);
    }
}

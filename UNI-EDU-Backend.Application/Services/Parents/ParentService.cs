using Mapster;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Exams;
using UNI_EDU_Backend.Application.DTOs.Parents;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Parents;

public class ParentService(
    IStudentRepository studentRepository,
    IExamRepository examRepository,
    IWalletRepository walletRepository,
    IChildProgressRepository childProgressRepository,
    INotificationRepository notificationRepository) : IParentService
{
    private const int ExamPageSize = 10;

    private readonly IStudentRepository _studentRepository = studentRepository;
    private readonly IExamRepository _examRepository = examRepository;
    private readonly IWalletRepository _walletRepository = walletRepository;
    private readonly IChildProgressRepository _childProgressRepository = childProgressRepository;
    private readonly INotificationRepository _notificationRepository = notificationRepository;

    public async Task<ChildProgressAnalyticsResponse> GetChildProgressAsync(Guid parentId, Guid childId, CancellationToken cancellationToken)
    {
        var children = await _studentRepository.GetByParentIdAsync(parentId, cancellationToken);
        if (!children.Any(c => c.StudentID == childId))
            throw new ForbiddenAccessException("Bạn không phải phụ huynh của học sinh này.");

        return await _childProgressRepository.GetChildProgressAsync(childId, cancellationToken);
    }

    public async Task<List<StudentSummaryResponse>> GetMyChildrenAsync(Guid parentId, CancellationToken cancellationToken)
    {
        var children = await _studentRepository.GetByParentIdAsync(parentId, cancellationToken);
        return children.Adapt<List<StudentSummaryResponse>>();
    }

    public async Task<PagedResult<SubmissionResponse>> GetChildExamsAsync(Guid parentId, Guid childId, int page, CancellationToken cancellationToken)
    {
        var children = await _studentRepository.GetByParentIdAsync(parentId, cancellationToken);
        if (!children.Any(c => c.StudentID == childId))
            throw new ForbiddenAccessException("Bạn không phải phụ huynh của học sinh này.");

        page = page < 1 ? 1 : page;
        var (items, total) = await _examRepository.GetSubmissionsByUserAsync(childId, page, ExamPageSize, cancellationToken);
        return new PagedResult<SubmissionResponse> { Items = items, Total = total, Page = page, PageSize = ExamPageSize };
    }

    public async Task FundChildAsync(Guid parentId, Guid childId, decimal amount, CancellationToken cancellationToken)
    {
        if (amount <= 0)
            throw new BadRequestException("Số tiền phải lớn hơn 0.");

        var children = await _studentRepository.GetByParentIdAsync(parentId, cancellationToken);
        var child = children.FirstOrDefault(c => c.StudentID == childId)
            ?? throw new ForbiddenAccessException("Bạn không phải phụ huynh của học sinh này.");

        var ok = await _walletRepository.TransferAsync(
            parentId, childId, amount,
            $"Nạp tiền cho con: {child.FullName}",
            "Phụ huynh nạp tiền vào ví của bạn",
            cancellationToken);
        if (!ok)
            throw new BadRequestException("Số dư ví của bạn không đủ. Vui lòng nạp tiền vào ví trước.");

        await _notificationRepository.CreateAsync(
            childId,
            "Nhận tiền từ phụ huynh",
            $"Phụ huynh đã nạp {amount:N0}đ vào ví của bạn để đóng học phí.",
            "success",
            "/student/wallet",
            cancellationToken);
    }
}

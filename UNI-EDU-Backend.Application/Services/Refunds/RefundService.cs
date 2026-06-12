using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Refunds;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Services.Refunds;

public class RefundService(
    IRefundRepository refundRepo,
    IAdminRepository adminRepo,
    IValidator<CreateRefundRequest> createValidator) : IRefundService
{
    private const int PageSize = 10;

    private readonly IRefundRepository _refundRepo = refundRepo;
    private readonly IAdminRepository _adminRepo = adminRepo;
    private readonly IValidator<CreateRefundRequest> _createValidator = createValidator;

    public async Task<RefundResponse> CreateAsync(Guid classId, CreateRefundRequest request, Guid callerUserId, string callerRole, CancellationToken cancellationToken)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        var ctx = await _refundRepo.GetClassForRefundAsync(classId, cancellationToken)
            ?? throw new NotFoundException($"Class with id '{classId}' not found.");

        var role = (callerRole ?? string.Empty).Trim();
        bool allowed = role switch
        {
            "Student" => ctx.StudentId == callerUserId,
            "Parent" => await _refundRepo.IsParentOfStudentAsync(callerUserId, ctx.StudentId, cancellationToken),
            _ => false
        };
        if (!allowed)
            throw new ForbiddenAccessException("Only the student or their parent can request a refund for this class.");

        if (ctx.RemainingEscrow <= 0m)
            throw new BadRequestException("There is no refundable escrow left on this class.");

        if (request.Amount > ctx.RemainingEscrow)
            throw new BadRequestException($"Amount exceeds the refundable balance ({ctx.RemainingEscrow:N0} VND).");

        return await _refundRepo.CreateAsync(classId, callerUserId, request.Amount, ctx.RemainingEscrow, request.Reason, cancellationToken);
    }

    public async Task<PagedResult<RefundResponse>> GetAllAsync(RefundListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var (items, total) = await _refundRepo.GetAllAsync(ParseStatus(query.Status), page, PageSize, cancellationToken);

        return new PagedResult<RefundResponse> { Items = items, Total = total, Page = page, PageSize = PageSize };
    }

    public async Task ApproveAsync(Guid refundId, ReviewRefundRequest request, Guid reviewerId, CancellationToken cancellationToken)
    {
        var result = await _refundRepo.ApproveAsync(refundId, reviewerId, request.Note, cancellationToken);
        EnsureReviewable(result.Outcome, refundId);
        await _adminRepo.AddAuditLogAsync(reviewerId, "Duyệt hoàn tiền", $"{result.StudentName} — {result.Amount:N0} VND", cancellationToken);
    }

    public async Task RejectAsync(Guid refundId, ReviewRefundRequest request, Guid reviewerId, CancellationToken cancellationToken)
    {
        var result = await _refundRepo.RejectAsync(refundId, reviewerId, request.Note, cancellationToken);
        EnsureReviewable(result.Outcome, refundId);
        await _adminRepo.AddAuditLogAsync(reviewerId, "Từ chối hoàn tiền", $"{result.StudentName} — {result.Amount:N0} VND", cancellationToken);
    }

    private static void EnsureReviewable(RefundReviewOutcome outcome, Guid refundId)
    {
        if (outcome == RefundReviewOutcome.NotFound)
            throw new NotFoundException($"Refund request with id '{refundId}' not found.");
        if (outcome == RefundReviewOutcome.AlreadyReviewed)
            throw new BadRequestException("This refund request has already been reviewed.");
    }

    private static RefundStatus? ParseStatus(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "pending" => RefundStatus.Pending,
            "approved" => RefundStatus.Approved,
            "rejected" => RefundStatus.Rejected,
            _ => null
        };
}

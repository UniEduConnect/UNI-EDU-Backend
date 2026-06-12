using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Admin;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Services.Admin;

public class AdminService(
    IAdminRepository adminRepo,
    IValidator<UpdateSystemSettingsRequest> settingsValidator,
    IValidator<CreateUserRequest> createUserValidator,
    IValidator<UpdateUserRequest> updateUserValidator) : IAdminService
{
    private const int PageSize = 10;
    private const int AuditPageSize = 20;

    private readonly IAdminRepository _adminRepo = adminRepo;
    private readonly IValidator<UpdateSystemSettingsRequest> _settingsValidator = settingsValidator;
    private readonly IValidator<CreateUserRequest> _createUserValidator = createUserValidator;
    private readonly IValidator<UpdateUserRequest> _updateUserValidator = updateUserValidator;

    public async Task<PagedResult<AdminUserResponse>> GetUsersAsync(AdminUserListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var (items, total) = await _adminRepo.GetUsersAsync(query, PageSize, cancellationToken);

        return new PagedResult<AdminUserResponse>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = PageSize
        };
    }

    public async Task<AdminUserResponse> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        await _adminRepo.GetUserByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"User with id '{userId}' not found.");

    public async Task<AdminUserResponse> CreateUserAsync(CreateUserRequest request, Guid adminId, CancellationToken cancellationToken)
    {
        await _createUserValidator.EnsureValidAsync(request, cancellationToken);

        if (await _adminRepo.EmailOrPhoneExistsAsync(request.Email.Trim(), request.PhoneNumber.Trim(), cancellationToken))
            throw new BadRequestException("Email or phone number already in use.");

        var hashed = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var created = await _adminRepo.CreateUserAsync(request, hashed, cancellationToken);
        await _adminRepo.AddAuditLogAsync(adminId, "Tạo tài khoản", $"{created.Name} ({created.Role})", cancellationToken);
        return created;
    }

    public async Task<AdminUserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request, Guid adminId, CancellationToken cancellationToken)
    {
        await _updateUserValidator.EnsureValidAsync(request, cancellationToken);

        var updated = await _adminRepo.UpdateUserAsync(userId, request, cancellationToken)
            ?? throw new NotFoundException($"User with id '{userId}' not found.");
        await _adminRepo.AddAuditLogAsync(adminId, "Cập nhật tài khoản", $"{updated.Name} ({updated.Role})", cancellationToken);
        return updated;
    }

    public async Task DeleteUserAsync(Guid userId, Guid adminId, CancellationToken cancellationToken)
    {
        if (userId == adminId)
            throw new BadRequestException("Bạn không thể xoá tài khoản của chính mình.");

        var user = await _adminRepo.GetUserByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"User with id '{userId}' not found.");
        var deleted = await _adminRepo.DeleteUserAsync(userId, cancellationToken);
        if (!deleted)
            throw new NotFoundException($"User with id '{userId}' not found.");
        await _adminRepo.AddAuditLogAsync(adminId, "Xoá tài khoản", $"{user.Name} ({user.Role})", cancellationToken);
    }

    public Task<AdminUserResponse> ApproveUserAsync(Guid userId, Guid adminId, CancellationToken cancellationToken) =>
        ChangeStatusAsync(userId, UserStatus.Approved, adminId, "Duyệt tài khoản", cancellationToken);

    public Task<AdminUserResponse> RejectUserAsync(Guid userId, RejectUserRequest request, Guid adminId, CancellationToken cancellationToken)
    {
        var action = string.IsNullOrWhiteSpace(request.Reason)
            ? "Từ chối tài khoản"
            : $"Từ chối tài khoản ({request.Reason.Trim()})";
        return ChangeStatusAsync(userId, UserStatus.Rejected, adminId, action, cancellationToken);
    }

    public Task<AdminUserResponse> SuspendUserAsync(Guid userId, Guid adminId, CancellationToken cancellationToken) =>
        ChangeStatusAsync(userId, UserStatus.Suspended, adminId, "Khoá tài khoản", cancellationToken);

    public async Task<PagedResult<AuditLogResponse>> GetAuditLogsAsync(int page, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        var (items, total) = await _adminRepo.GetAuditLogsAsync(page, AuditPageSize, cancellationToken);

        return new PagedResult<AuditLogResponse>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = AuditPageSize
        };
    }

    public Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken) =>
        _adminRepo.GetDashboardAsync(cancellationToken);

    public Task<SystemSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken) =>
        _adminRepo.GetSettingsAsync(cancellationToken);

    public async Task<SystemSettingsResponse> UpdateSettingsAsync(UpdateSystemSettingsRequest request, Guid adminId, CancellationToken cancellationToken)
    {
        await _settingsValidator.EnsureValidAsync(request, cancellationToken);

        var result = await _adminRepo.UpdateSettingsAsync(request, cancellationToken);
        await _adminRepo.AddAuditLogAsync(adminId, "Cập nhật cấu hình hệ thống", request.PlatformName, cancellationToken);
        return result;
    }

    private async Task<AdminUserResponse> ChangeStatusAsync(Guid userId, UserStatus status, Guid adminId, string action, CancellationToken cancellationToken)
    {
        var user = await _adminRepo.SetUserStatusAsync(userId, status, cancellationToken)
            ?? throw new NotFoundException($"User with id '{userId}' not found.");

        await _adminRepo.AddAuditLogAsync(adminId, action, $"{user.Name} ({user.Role})", cancellationToken);

        return user;
    }
}

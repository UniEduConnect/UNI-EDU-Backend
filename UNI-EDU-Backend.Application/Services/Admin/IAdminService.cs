using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Admin;

namespace UNI_EDU_Backend.Application.Services.Admin;

public interface IAdminService
{
    Task<PagedResult<AdminUserResponse>> GetUsersAsync(AdminUserListQuery query, CancellationToken cancellationToken);
    Task<AdminUserResponse> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<AdminUserResponse> CreateUserAsync(CreateUserRequest request, Guid adminId, CancellationToken cancellationToken);
    Task<AdminUserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request, Guid adminId, CancellationToken cancellationToken);
    Task DeleteUserAsync(Guid userId, Guid adminId, CancellationToken cancellationToken);

    Task<AdminUserResponse> ApproveUserAsync(Guid userId, Guid adminId, CancellationToken cancellationToken);
    Task<AdminUserResponse> RejectUserAsync(Guid userId, RejectUserRequest request, Guid adminId, CancellationToken cancellationToken);
    Task<AdminUserResponse> SuspendUserAsync(Guid userId, Guid adminId, CancellationToken cancellationToken);

    Task<PagedResult<AuditLogResponse>> GetAuditLogsAsync(int page, CancellationToken cancellationToken);

    Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken);
    Task<SystemSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken);
    Task<SystemSettingsResponse> UpdateSettingsAsync(UpdateSystemSettingsRequest request, Guid adminId, CancellationToken cancellationToken);
}

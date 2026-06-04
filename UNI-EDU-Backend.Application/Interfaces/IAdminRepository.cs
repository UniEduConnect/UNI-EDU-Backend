using UNI_EDU_Backend.Application.DTOs.Admin;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IAdminRepository
{
    Task<(List<AdminUserResponse> Items, int Total)> GetUsersAsync(AdminUserListQuery query, int pageSize, CancellationToken cancellationToken);
    Task<AdminUserResponse?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken);

    // Flips the moderation status; returns the updated row, or null if the user doesn't exist.
    Task<AdminUserResponse?> SetUserStatusAsync(Guid userId, UserStatus status, CancellationToken cancellationToken);

    // Appends an audit entry. ActorName is resolved from the actor's User row when found.
    Task AddAuditLogAsync(Guid? actorUserId, string action, string target, CancellationToken cancellationToken);
    Task<(List<AuditLogResponse> Items, int Total)> GetAuditLogsAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken);

    // Reads the single settings row, creating defaults on first access.
    Task<SystemSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken);
    Task<SystemSettingsResponse> UpdateSettingsAsync(UpdateSystemSettingsRequest request, CancellationToken cancellationToken);
}

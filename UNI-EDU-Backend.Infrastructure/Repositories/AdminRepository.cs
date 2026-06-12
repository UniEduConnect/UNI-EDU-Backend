using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Admin;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class AdminRepository(ApplicationDbContext dbContext) : IAdminRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<(List<AdminUserResponse> Items, int Total)> GetUsersAsync(AdminUserListQuery query, int pageSize, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var q = _dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(u => EF.Functions.ILike(u.Fullname, $"%{term}%") || EF.Functions.ILike(u.Email, $"%{term}%"));
        }

        q = ApplyRoleFilter(q, query.Role);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = ParseStatus(query.Status);
            if (status is not null)
                q = q.Where(u => u.Status == status);
        }

        var total = await q.CountAsync(cancellationToken);

        var rows = await q
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ProjectRow)
            .ToListAsync(cancellationToken);

        return (rows.Select(Map).ToList(), total);
    }

    public async Task<AdminUserResponse?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.UserID == userId)
            .Select(ProjectRow)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : Map(row);
    }

    public async Task<AdminUserResponse?> SetUserStatusAsync(Guid userId, UserStatus status, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);
        if (user is null) return null;

        user.Status = status;

        // Keep the tutor's verification badge in sync with the account decision:
        // approving a tutor verifies them; rejecting/suspending/pending un-verifies.
        if (user.Role == UserRole.Tutor)
        {
            var tutor = await _dbContext.Tutors.FirstOrDefaultAsync(t => t.TutorID == userId, cancellationToken);
            if (tutor is not null)
                tutor.IsVerified = status == UserStatus.Approved;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetUserByIdAsync(userId, cancellationToken);
    }

    public Task<bool> EmailOrPhoneExistsAsync(string email, string phoneNumber, CancellationToken cancellationToken) =>
        _dbContext.Users.AnyAsync(u => u.Email == email || u.PhoneNumber == phoneNumber, cancellationToken);

    public async Task<AdminUserResponse> CreateUserAsync(CreateUserRequest request, string hashedPassword, CancellationToken cancellationToken)
    {
        var role = ParseRole(request.Role);
        var userId = Guid.NewGuid();

        var user = new User
        {
            UserID = userId,
            Email = request.Email.Trim(),
            HashedPassword = hashedPassword,
            PhoneNumber = request.PhoneNumber.Trim(),
            Fullname = request.Fullname.Trim(),
            Role = role,
            Status = UserStatus.Approved, // admin-created accounts are active immediately
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);

        switch (role)
        {
            case UserRole.Student:
                _dbContext.Students.Add(new Student { StudentID = userId, FullName = user.Fullname, School = request.School ?? string.Empty, Grade = request.Grade ?? 0 });
                break;
            case UserRole.Parent:
                _dbContext.Parents.Add(new Parent { ParentID = userId, FullName = user.Fullname });
                break;
            case UserRole.Tutor:
                var isTeacher = (request.Role ?? string.Empty).Trim().ToLowerInvariant() == "teacher";
                _dbContext.Tutors.Add(new Tutor { TutorID = userId, FullName = user.Fullname, Gender = request.Gender ?? string.Empty, Degree = request.Degree ?? string.Empty, StudentIdNumber = request.StudentIdNumber, TutorType = isTeacher ? TutorType.Teacher : TutorType.Tutor, AverageRating = 0 });
                break;
            // Admin / Office / Finance / ExamManager: User row only, no sub-entity.
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetUserByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User creation failed.");
    }

    public async Task<AdminUserResponse?> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(u => u.Tutor).Include(u => u.Parent).Include(u => u.Student)
            .FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);
        if (user is null) return null;

        if (!string.IsNullOrWhiteSpace(request.Fullname))
        {
            user.Fullname = request.Fullname.Trim();
            if (user.Tutor != null) user.Tutor.FullName = user.Fullname;
            if (user.Parent != null) user.Parent.FullName = user.Fullname;
            if (user.Student != null) user.Student.FullName = user.Fullname;
        }
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            user.PhoneNumber = request.PhoneNumber.Trim();

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim();
            if (!string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase)
                && await _dbContext.Users.AnyAsync(u => u.UserID != userId && u.Email == email, cancellationToken))
                throw new BadRequestException("Email đã được sử dụng bởi tài khoản khác.");
            user.Email = email;
        }

        // Student-specific
        if (user.Student != null)
        {
            if (request.School != null) user.Student.School = request.School.Trim();
            if (request.Grade is not null) user.Student.Grade = request.Grade.Value;
        }
        // Tutor / Teacher-specific
        if (user.Tutor != null)
        {
            if (request.School != null) user.Tutor.School = request.School.Trim();
            if (request.Gender != null) user.Tutor.Gender = request.Gender.Trim();
            if (request.Degree != null) user.Tutor.Degree = request.Degree.Trim();
            if (request.StudentIdNumber != null) user.Tutor.StudentIdNumber = request.StudentIdNumber.Trim();
            if (request.Bio != null) user.Tutor.Bio = request.Bio;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetUserByIdAsync(userId, cancellationToken);
    }

    public async Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);
        if (user is null) return false;

        _dbContext.Users.Remove(user); // sub-rows / wallet cascade via FK
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static UserRole ParseRole(string? role) => (role ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "admin" => UserRole.Admin,
        "tutor" or "teacher" => UserRole.Tutor,
        "parent" => UserRole.Parent,
        "office" => UserRole.Office,
        "finance" => UserRole.Finance,
        "exam-manager" or "exammanager" => UserRole.ExamManager,
        _ => UserRole.Student
    };

    public async Task AddAuditLogAsync(Guid? actorUserId, string action, string target, CancellationToken cancellationToken)
    {
        // Resolve the actor; if the user no longer exists (e.g. was deleted),
        // store ActorUserId = null so the FK can never be violated.
        var actorName = "System";
        Guid? resolvedActorId = null;
        if (actorUserId is not null)
        {
            var name = await _dbContext.Users
                .Where(u => u.UserID == actorUserId)
                .Select(u => u.Fullname)
                .FirstOrDefaultAsync(cancellationToken);
            if (name is not null) { resolvedActorId = actorUserId; actorName = name; }
            else { actorName = "Unknown"; }
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            AuditLogID = Guid.NewGuid(),
            ActorUserId = resolvedActorId,
            ActorName = actorName,
            Action = action,
            Target = target,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(List<AuditLogResponse> Items, int Total)> GetAuditLogsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var q = _dbContext.AuditLogs.AsNoTracking();
        var total = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogResponse
            {
                Id = a.AuditLogID,
                Actor = a.ActorName,
                Action = a.Action,
                Target = a.Target,
                Timestamp = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var totalUsers = await _dbContext.Users.CountAsync(cancellationToken);
        var tutors = await _dbContext.Tutors.CountAsync(t => t.TutorType == TutorType.Tutor || t.TutorType == null, cancellationToken);
        var teachers = await _dbContext.Tutors.CountAsync(t => t.TutorType == TutorType.Teacher, cancellationToken);
        var students = await _dbContext.Users.CountAsync(u => u.Role == UserRole.Student, cancellationToken);
        var parents = await _dbContext.Users.CountAsync(u => u.Role == UserRole.Parent, cancellationToken);
        var pendingApprovals = await _dbContext.Users.CountAsync(u => u.Status == UserStatus.Pending, cancellationToken);

        var totalClasses = await _dbContext.Classes.CountAsync(cancellationToken);
        var activeClasses = await _dbContext.Classes.CountAsync(c => c.Status == ClassStatus.Active, cancellationToken);

        var totalExams = await _dbContext.Exams.CountAsync(cancellationToken);

        var totalRevenue = await _dbContext.WalletTransactions
            .Where(t => t.Type == WalletTxType.EscrowIn && t.Status == WalletTxStatus.Completed)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var pendingWithdrawals = await _dbContext.Withdrawals.CountAsync(w => w.Status == WithdrawalStatus.Pending, cancellationToken);
        var openIncidents = await _dbContext.Incidents.CountAsync(i => i.Status != IncidentStatus.Resolved, cancellationToken);

        return new DashboardResponse
        {
            TotalUsers = totalUsers,
            Tutors = tutors,
            Teachers = teachers,
            Students = students,
            Parents = parents,
            PendingApprovals = pendingApprovals,
            TotalClasses = totalClasses,
            ActiveClasses = activeClasses,
            TotalExams = totalExams,
            TotalRevenue = totalRevenue,
            PendingWithdrawals = pendingWithdrawals,
            OpenIncidents = openIncidents
        };
    }

    public async Task<SystemSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        return MapSettings(settings);
    }

    public async Task<SystemSettingsResponse> UpdateSettingsAsync(UpdateSystemSettingsRequest request, CancellationToken cancellationToken)
    {
        var s = await GetOrCreateSettingsAsync(cancellationToken);

        s.PlatformName = request.PlatformName.Trim();
        s.EscrowPercent = request.EscrowPercent;
        s.EscrowHoldDays = request.EscrowHoldDays;
        s.EnableExams = request.EnableExams;
        s.EnableChat = request.EnableChat;
        s.EnablePayments = request.EnablePayments;
        s.MaintenanceMode = request.MaintenanceMode;
        s.EmailNotifications = request.EmailNotifications;
        s.SmsNotifications = request.SmsNotifications;
        s.PushNotifications = request.PushNotifications;
        s.TwoFactorAuth = request.TwoFactorAuth;
        s.SessionTimeout = request.SessionTimeout;
        s.MaxLoginAttempts = request.MaxLoginAttempts;
        s.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapSettings(s);
    }

    private async Task<SystemSetting> GetOrCreateSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.SystemSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new SystemSetting { SettingID = Guid.NewGuid(), UpdatedAt = DateTime.UtcNow };
            _dbContext.SystemSettings.Add(settings);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        return settings;
    }

    private static SystemSettingsResponse MapSettings(SystemSetting s) => new()
    {
        PlatformName = s.PlatformName,
        EscrowPercent = s.EscrowPercent,
        EscrowHoldDays = s.EscrowHoldDays,
        EnableExams = s.EnableExams,
        EnableChat = s.EnableChat,
        EnablePayments = s.EnablePayments,
        MaintenanceMode = s.MaintenanceMode,
        EmailNotifications = s.EmailNotifications,
        SmsNotifications = s.SmsNotifications,
        PushNotifications = s.PushNotifications,
        TwoFactorAuth = s.TwoFactorAuth,
        SessionTimeout = s.SessionTimeout,
        MaxLoginAttempts = s.MaxLoginAttempts,
        UpdatedAt = s.UpdatedAt
    };

    private static IQueryable<User> ApplyRoleFilter(IQueryable<User> q, string? role)
    {
        switch ((role ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "admin": return q.Where(u => u.Role == UserRole.Admin);
            case "parent": return q.Where(u => u.Role == UserRole.Parent);
            case "student": return q.Where(u => u.Role == UserRole.Student);
            case "tutor": return q.Where(u => u.Role == UserRole.Tutor && u.Tutor != null && u.Tutor.TutorType == TutorType.Tutor);
            case "teacher": return q.Where(u => u.Role == UserRole.Tutor && u.Tutor != null && u.Tutor.TutorType == TutorType.Teacher);
            case "office": return q.Where(u => u.Role == UserRole.Office);
            case "finance": return q.Where(u => u.Role == UserRole.Finance);
            case "exam-manager" or "exammanager": return q.Where(u => u.Role == UserRole.ExamManager);
            default: return q;
        }
    }

    private static UserStatus? ParseStatus(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "pending" => UserStatus.Pending,
            "approved" => UserStatus.Approved,
            "rejected" => UserStatus.Rejected,
            "suspended" => UserStatus.Suspended,
            _ => null
        };

    // Raw row pulled from the DB (enums intact) before in-memory string mapping.
    private record UserRow(
        Guid Id, string Name, string Email, string Phone, UserRole Role, UserStatus Status,
        DateTime CreatedAt, TutorType? TutorType, string? Avatar, string? TutorSchool, string? StudentSchool, string? Bio,
        int? Grade, string? Gender, string? Degree, string? StudentIdNumber);

    private static readonly System.Linq.Expressions.Expression<Func<User, UserRow>> ProjectRow =
        u => new UserRow(
            u.UserID,
            u.Fullname,
            u.Email,
            u.PhoneNumber,
            u.Role,
            u.Status,
            u.CreatedAt,
            u.Tutor != null ? (TutorType?)u.Tutor.TutorType : null,
            u.Tutor != null ? u.Tutor.AvatarUrl : null,
            u.Tutor != null ? u.Tutor.School : null,
            u.Student != null ? u.Student.School : null,
            u.Tutor != null ? u.Tutor.Bio : null,
            u.Student != null ? (int?)u.Student.Grade : null,
            u.Tutor != null ? u.Tutor.Gender : null,
            u.Tutor != null ? u.Tutor.Degree : null,
            u.Tutor != null ? u.Tutor.StudentIdNumber : null);

    private static AdminUserResponse Map(UserRow r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Email = r.Email,
        Phone = r.Phone,
        Role = RoleToWire(r.Role, r.TutorType),
        Status = r.Status.ToString().ToLowerInvariant(),
        Avatar = r.Avatar,
        School = r.TutorSchool ?? r.StudentSchool,
        Bio = r.Bio,
        Grade = r.Grade,
        Gender = r.Gender,
        Degree = r.Degree,
        StudentIdNumber = r.StudentIdNumber,
        CreatedAt = r.CreatedAt
    };

    private static string RoleToWire(UserRole role, TutorType? tutorType) => role switch
    {
        UserRole.Admin => "admin",
        UserRole.Parent => "parent",
        UserRole.Student => "student",
        UserRole.Tutor => tutorType == TutorType.Teacher ? "teacher" : "tutor",
        UserRole.Office => "office",
        UserRole.Finance => "finance",
        UserRole.ExamManager => "exam-manager",
        _ => role.ToString().ToLowerInvariant()
    };
}

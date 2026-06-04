using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Profile;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class ProfileRepository(ApplicationDbContext dbContext) : IProfileRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);
        if (user is null) return null;

        var resp = new CurrentUserResponse
        {
            Id = user.UserID,
            Fullname = user.Fullname,
            Email = user.Email,
            Phone = user.PhoneNumber,
            Role = user.Role.ToString().ToLowerInvariant(),
            Status = user.Status.ToString().ToLowerInvariant(),
            CreatedAt = user.CreatedAt
        };

        switch (user.Role)
        {
            case UserRole.Tutor:
                var tutor = await _dbContext.Tutors.AsNoTracking()
                    .Include(t => t.Subjects)
                    .FirstOrDefaultAsync(t => t.TutorID == userId, cancellationToken);
                if (tutor is not null)
                {
                    resp.Role = tutor.TutorType == Domain.Enums.TutorType.Teacher ? "teacher" : "tutor";
                    resp.Tutor = new MeTutorProfile
                    {
                        AvatarUrl = tutor.AvatarUrl,
                        Bio = tutor.Bio,
                        School = tutor.School,
                        Location = tutor.Location,
                        Degree = tutor.Degree,
                        Gender = tutor.Gender,
                        DateOfBirth = tutor.DateOfBirth,
                        HourlyRate = tutor.HourlyRate,
                        YearsExperience = tutor.YearsExperience,
                        TeachingStyle = tutor.TeachingStyle,
                        IntroVideoUrl = tutor.IntroVideoUrl,
                        IsVerified = tutor.IsVerified ?? false,
                        AverageRating = tutor.AverageRating,
                        TutorType = tutor.TutorType == Domain.Enums.TutorType.Teacher ? "teacher" : "tutor",
                        Subjects = tutor.Subjects.Select(s => s.SubjectName).ToList(),
                        Certificates = tutor.Certificates ?? [],
                        Achievements = tutor.Achievements ?? []
                    };
                }
                break;

            case UserRole.Student:
                var student = await _dbContext.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentID == userId, cancellationToken);
                if (student is not null)
                    resp.Student = new MeStudentProfile { School = student.School, Grade = student.Grade, ParentId = student.ParentID };
                break;

            case UserRole.Parent:
                var childrenCount = await _dbContext.Students.CountAsync(s => s.ParentID == userId, cancellationToken);
                resp.Parent = new MeParentProfile { ChildrenCount = childrenCount };
                break;
        }

        return resp;
    }

    public async Task<bool> UpdateUserCommonAsync(Guid userId, string? fullname, string? phoneNumber, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);
        if (user is null) return false;

        if (fullname is not null) user.Fullname = fullname.Trim();
        if (phoneNumber is not null) user.PhoneNumber = phoneNumber.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> TutorExistsAsync(Guid tutorId, CancellationToken cancellationToken) =>
        _dbContext.Tutors.AnyAsync(t => t.TutorID == tutorId, cancellationToken);

    public async Task<List<Guid>> GetMissingSubjectIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var distinct = ids.Distinct().ToList();
        var existing = await _dbContext.Subjects
            .Where(s => distinct.Contains(s.SubjectID))
            .Select(s => s.SubjectID)
            .ToListAsync(cancellationToken);
        return distinct.Except(existing).ToList();
    }

    public async Task<bool> UpdateTutorProfileAsync(Guid tutorId, UpdateTutorProfileRequest request, CancellationToken cancellationToken)
    {
        var tutor = await _dbContext.Tutors
            .Include(t => t.Subjects)
            .FirstOrDefaultAsync(t => t.TutorID == tutorId, cancellationToken);
        if (tutor is null) return false;

        if (request.Bio is not null) tutor.Bio = request.Bio;
        if (request.AvatarUrl is not null) tutor.AvatarUrl = request.AvatarUrl;
        if (request.Location is not null) tutor.Location = request.Location;
        if (request.School is not null) tutor.School = request.School;
        if (request.Degree is not null) tutor.Degree = request.Degree;
        if (request.Gender is not null) tutor.Gender = request.Gender;
        if (request.DateOfBirth is not null) tutor.DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth.Value, DateTimeKind.Utc);
        if (request.HourlyRate is not null) tutor.HourlyRate = request.HourlyRate;
        if (request.YearsExperience is not null) tutor.YearsExperience = request.YearsExperience;
        if (request.TeachingStyle is not null) tutor.TeachingStyle = request.TeachingStyle;
        if (request.IntroVideoUrl is not null) tutor.IntroVideoUrl = request.IntroVideoUrl;
        if (request.Certificates is not null) tutor.Certificates = request.Certificates;
        if (request.Achievements is not null) tutor.Achievements = request.Achievements;
        if (request.TutorType is not null)
            tutor.TutorType = request.TutorType.Trim().ToLowerInvariant() == "teacher"
                ? Domain.Enums.TutorType.Teacher
                : Domain.Enums.TutorType.Tutor;

        if (request.SubjectIds is not null)
        {
            var desired = request.SubjectIds.Distinct().ToHashSet();
            var existingLinks = await _dbContext.TutorSubjects
                .Where(ts => ts.TutorID == tutorId)
                .ToListAsync(cancellationToken);

            var toRemove = existingLinks.Where(l => !desired.Contains(l.SubjectID)).ToList();
            _dbContext.TutorSubjects.RemoveRange(toRemove);

            var existingIds = existingLinks.Select(l => l.SubjectID).ToHashSet();
            foreach (var sid in desired.Where(id => !existingIds.Contains(id)))
                _dbContext.TutorSubjects.Add(new TutorSubject { TutorID = tutorId, SubjectID = sid });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<StudentProfileResponse?> GetStudentProfileAsync(Guid studentId, CancellationToken cancellationToken) =>
        _dbContext.Students.AsNoTracking()
            .Where(s => s.StudentID == studentId)
            .Select(s => new StudentProfileResponse
            {
                Id = s.StudentID,
                FullName = s.FullName,
                School = s.School,
                Grade = s.Grade,
                ParentId = s.ParentID
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> UpdateStudentProfileAsync(Guid studentId, UpdateStudentProfileRequest request, CancellationToken cancellationToken)
    {
        var student = await _dbContext.Students.FirstOrDefaultAsync(s => s.StudentID == studentId, cancellationToken);
        if (student is null) return false;

        if (request.FullName is not null) student.FullName = request.FullName.Trim();
        if (request.School is not null) student.School = request.School.Trim();
        if (request.Grade is not null) student.Grade = request.Grade.Value;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<ParentProfileResponse?> GetParentProfileAsync(Guid parentId, CancellationToken cancellationToken) =>
        _dbContext.Parents.AsNoTracking()
            .Where(p => p.ParentID == parentId)
            .Select(p => new ParentProfileResponse
            {
                Id = p.ParentID,
                FullName = p.FullName,
                ChildrenCount = _dbContext.Students.Count(s => s.ParentID == p.ParentID)
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> UpdateParentProfileAsync(Guid parentId, UpdateParentProfileRequest request, CancellationToken cancellationToken)
    {
        var parent = await _dbContext.Parents.FirstOrDefaultAsync(p => p.ParentID == parentId, cancellationToken);
        if (parent is null) return false;

        if (request.FullName is not null) parent.FullName = request.FullName.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<ScheduleItemResponse>> GetMyScheduleAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        var q = _dbContext.Sessions.AsNoTracking().AsQueryable();

        var trimmedRole = (role ?? string.Empty).Trim();
        q = trimmedRole switch
        {
            "Tutor" => q.Where(s => s.Class.TutorID == userId),
            "Student" => q.Where(s => s.Class.StudentID == userId),
            "Parent" => q.Where(s => s.Class.Student.ParentID == userId),
            _ => q // Admin: all sessions
        };

        var rows = await q
            .OrderBy(s => s.StartAt)
            .Select(s => new
            {
                s.SessionID,
                s.ClassID,
                ClassName = s.Class.Name,
                Subject = s.Class.Subject.SubjectName,
                TutorName = s.Class.Tutor.FullName ?? s.Class.Tutor.User.Fullname,
                StudentName = s.Class.Student.FullName ?? s.Class.Student.User.Fullname,
                s.StartAt,
                s.EndAt,
                s.Status,
                s.Format
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new ScheduleItemResponse
        {
            SessionId = r.SessionID,
            ClassId = r.ClassID,
            ClassName = r.ClassName ?? string.Empty,
            Subject = r.Subject ?? string.Empty,
            CounterpartName = trimmedRole == "Tutor" ? (r.StudentName ?? string.Empty) : (r.TutorName ?? string.Empty),
            StartAt = r.StartAt,
            EndAt = r.EndAt,
            Status = r.Status.ToString().ToLowerInvariant(),
            Format = r.Format.ToString().ToLowerInvariant()
        }).ToList();
    }
}

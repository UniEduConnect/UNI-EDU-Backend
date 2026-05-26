using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Tutors;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class TutorRepository : GenericRepository<Tutor>, ITutorRepository
{
    private readonly ApplicationDbContext _dbContext;
    public TutorRepository(ApplicationDbContext context) : base(context)
    {
        this._dbContext = context;
    }

    public async Task<(List<TutorListingResponse> Items, int Total)> SearchAsync(
    TutorSearchQuery query,
    int pageSize,
    CancellationToken cancellationToken)
    {
        var tutors = _dbContext.Tutors.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            tutors = tutors.Where(t =>
                EF.Functions.ILike(t.FullName, pattern) ||
                t.Subjects.Any(s => EF.Functions.ILike(s.SubjectName, pattern)));
        }

        if (!string.IsNullOrWhiteSpace(query.Subject))
        {
            var subject = query.Subject.Trim();
            tutors = tutors.Where(t => t.Subjects.Any(s => s.SubjectName == subject));
        }

        var typeFilter = (query.Type ?? "all").ToLowerInvariant();
        if (typeFilter == "tutor")
            tutors = tutors.Where(t => t.TutorType == TutorType.Tutor);
        else if (typeFilter == "teacher")
            tutors = tutors.Where(t => t.TutorType == TutorType.Teacher);

        tutors = tutors.Where(t => t.HourlyRate >= query.MinPrice && t.HourlyRate <= query.MaxPrice);

        var total = await tutors.CountAsync(cancellationToken);
        var skip = (query.Page - 1) * pageSize;

        // 1. Handle all nullable fields directly in the database projection using ??
        var raw = await tutors
            .OrderByDescending(t => t.AverageRating)
            .ThenBy(t => t.TutorID)
            .Skip(skip)
            .Take(pageSize)
            .Select(t => new
            {
                t.TutorID,
                FullName = t.FullName ?? string.Empty,
                AvatarUrl = t.AvatarUrl ?? string.Empty,
                SubjectNames = t.Subjects.Select(s => s.SubjectName).ToList(),
                AverageRating = t.AverageRating ?? 0,
                TotalReviews = _dbContext.Reviews.Count(r => r.TutorID == t.TutorID),
                TotalSessions = _dbContext.Classes.Count(c => c.TutorID == t.TutorID),
                t.YearsExperience,
                t.HourlyRate,
                t.Location,
                t.IsVerified,
                t.Bio,
                t.School,
                t.Degree,
                t.TutorType,
                // EF cannot translate operations over the jsonb-converted list — pull it raw
                // and map to DTOs client-side via MapSlots after materialization.
                t.AvailableSlots,

                Certificates = t.Certificates ?? new List<string>(),
                IntroVideoUrl = t.IntroVideoUrl ?? string.Empty,
                TeachingStyle = t.TeachingStyle ?? string.Empty,
                Achievements = t.Achievements ?? new List<string>()
            })
            .ToListAsync(cancellationToken);

        var items = raw.Select(t => new TutorListingResponse
        {
            Id = t.TutorID,
            Name = t.FullName,
            Avatar = t.AvatarUrl,
            Subjects = t.SubjectNames,
            Rating = t.AverageRating,
            TotalReviews = t.TotalReviews,
            TotalSessions = t.TotalSessions,
            YearsExperience = t.YearsExperience ?? 0,
            HourlyRate = t.HourlyRate ?? 0,
            Location = t.Location,
            Verified = t.IsVerified ?? false,
            Bio = t.Bio,
            School = t.School,
            Degree = t.Degree,
            Type = t.TutorType == TutorType.Teacher ? "teacher" : "tutor",
            AvailableSlots = MapSlots(t.AvailableSlots),
            Certificates = t.Certificates,
            IntroVideoUrl = t.IntroVideoUrl,
            TeachingStyle = t.TeachingStyle,
            Achievements = t.Achievements
        }).ToList();

        return (items, total);
    }

    private static List<AvailableSlotDto> MapSlots(List<AvailableSlot>? slots) =>
        slots == null
            ? new List<AvailableSlotDto>()
            : slots.Select(a => new AvailableSlotDto { Day = a.Day, Time = a.Time }).ToList();

    public async Task<TutorProfileResponse?> GetProfileByIdAsync(Guid tutorId, int recentReviewCount, CancellationToken cancellationToken)
    {
        var raw = await _dbContext.Tutors
            .AsNoTracking()
            .Where(t => t.TutorID == tutorId)
            .Select(t => new
            {
                t.TutorID,
                FullName = t.FullName ?? string.Empty,
                AvatarUrl = t.AvatarUrl ?? string.Empty,
                Bio = t.Bio ?? string.Empty,
                School = t.School ?? string.Empty,
                Degree = t.Degree ?? string.Empty,
                IntroVideoUrl = t.IntroVideoUrl ?? string.Empty,
                AverageRating = t.AverageRating ?? 0,
                HourlyRate = t.HourlyRate ?? 0,
                Location = t.Location ?? string.Empty,
                TeachingStyle = t.TeachingStyle ?? string.Empty,
                YearsExperience = t.YearsExperience ?? 0,
                IsVerified = t.IsVerified ?? false,
                t.TutorType,
                // EF cannot translate operations over the jsonb-converted list — pull it raw
                // and map to DTOs client-side via MapSlots after materialization.
                t.AvailableSlots,

                Achievements = t.Achievements ?? new List<string>(),
                Email = t.User != null ? (t.User.Email ?? string.Empty) : string.Empty,
                Phone = t.User != null ? (t.User.PhoneNumber ?? string.Empty) : string.Empty,
                JoinDate = t.User != null ? t.User.CreatedAt : DateTime.UtcNow,
                SubjectNames = t.Subjects.Select(s => s.SubjectName).ToList(),
                TotalReviews = _dbContext.Reviews.Count(r => r.TutorID == t.TutorID),
                TotalSessions = _dbContext.Classes.Count(c => c.TutorID == t.TutorID)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (raw == null)
            return null;

        var rawReviews = await _dbContext.Reviews
            .AsNoTracking()
            .Where(r => r.TutorID == tutorId)
            .OrderByDescending(r => r.ReviewDate)
            .Take(recentReviewCount)
            .Select(r => new TutorReviewResponse
            {
                Id = r.ReviewID,
                ClassId = r.ClassID,
                ClassName = r.Class.Subject.SubjectName,
                StudentName = r.Reviewer.Fullname,
                ParentName = r.Reviewer.Fullname,
                Rating = r.Rating,
                Comment = r.Comment,
                Date = r.ReviewDate.ToString("yyyy-MM-dd"),
                Avatar = string.Empty,
                Subject = r.Class.Subject.SubjectName
            })
            .ToListAsync(cancellationToken);

        return new TutorProfileResponse
        {
            Id = raw.TutorID,
            Name = raw.FullName,
            Avatar = raw.AvatarUrl,
            Email = raw.Email,
            Phone = raw.Phone,
            Subjects = raw.SubjectNames,
            Bio = raw.Bio,
            School = raw.School,
            Degree = raw.Degree,
            DegreeVerified = raw.IsVerified ,
            TranscriptVerified = raw.IsVerified,
            VideoUrl = raw.IntroVideoUrl,
            Rating = raw.AverageRating,
            TotalReviews = raw.TotalReviews,
            TotalSessions = raw.TotalSessions,
            TestPassRate = 0,
            HourlyRate = raw.HourlyRate,
            Availability = GroupAvailability(MapSlots(raw.AvailableSlots)),
            JoinDate = raw.JoinDate.ToString("yyyy-MM-dd"),
            Location = raw.Location,
            TeachingStyle = raw.TeachingStyle,
            Achievements = raw.Achievements,
            Role = raw.TutorType == TutorType.Teacher ? "teacher" : "tutor",
            YearsExperience = raw.YearsExperience,
            CurrentSchool = null,
            PlatformFeeRate = 0m,
            Reviews = rawReviews
        };
    }

    private static List<AvailabilityDayDto> GroupAvailability(List<AvailableSlotDto>? slots)
    {
        if (slots == null || slots.Count == 0)
            return new List<AvailabilityDayDto>();

        return slots
            .GroupBy(a => a.Day)
            .Select(g => new AvailabilityDayDto
            {
                Day = g.Key,
                Slots = g.Select(s => s.Time).ToList()
            })
            .ToList();
    }

    public Task<bool> ExistsAsync(Guid tutorId, CancellationToken cancellationToken) =>
        _dbContext.Tutors.AnyAsync(t => t.TutorID == tutorId, cancellationToken);

    public async Task<(List<TutorReviewResponse> Items, int Total)> GetReviewsByTutorIdAsync(Guid tutorId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var reviews = _dbContext.Reviews.AsNoTracking().Where(r => r.TutorID == tutorId);

        var total = await reviews.CountAsync(cancellationToken);

        var skip = (page - 1) * pageSize;

        var items = await reviews
            .OrderByDescending(r => r.ReviewDate)
            .ThenByDescending(r => r.ReviewID)
            .Skip(skip)
            .Take(pageSize)
            .Select(r => new TutorReviewResponse
            {
                Id = r.ReviewID,
                ClassId = r.ClassID,
                ClassName = r.Class.Subject.SubjectName,
                StudentName = r.Reviewer.Fullname,
                ParentName = r.Reviewer.Fullname,
                Rating = r.Rating,
                Comment = r.Comment,
                Date = r.ReviewDate.ToString("yyyy-MM-dd"),
                Avatar = string.Empty,
                Subject = r.Class.Subject.SubjectName
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
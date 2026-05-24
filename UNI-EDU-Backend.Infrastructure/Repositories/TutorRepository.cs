using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.DTOs.Tutors;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class TutorRepository(ApplicationDbContext dbContext) : ITutorRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

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

        var raw = await tutors
            .OrderByDescending(t => t.AverageRating)
            .ThenBy(t => t.TutorID)
            .Skip(skip)
            .Take(pageSize)
            .Select(t => new
            {
                t.TutorID,
                t.FullName,
                t.AvatarUrl,
                SubjectNames = t.Subjects.Select(s => s.SubjectName).ToList(),
                t.AverageRating,
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
                t.AvailableSlots,
                t.Certificates,
                t.IntroVideoUrl,
                t.TeachingStyle,
                t.Achievements
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
            YearsExperience = t.YearsExperience,
            HourlyRate = t.HourlyRate,
            Location = t.Location,
            Verified = t.IsVerified,
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
                t.FullName,
                t.AvatarUrl,
                t.Bio,
                t.School,
                t.Degree,
                t.IntroVideoUrl,
                t.AverageRating,
                t.HourlyRate,
                t.Location,
                t.TeachingStyle,
                t.YearsExperience,
                t.IsVerified,
                t.TutorType,
                t.AvailableSlots,
                t.Achievements,
                Email = t.User.Email,
                Phone = t.User.PhoneNumber,
                JoinDate = t.User.CreatedAt,
                SubjectNames = t.Subjects.Select(s => s.SubjectName).ToList(),
                TotalReviews = _dbContext.Reviews.Count(r => r.TutorID == t.TutorID),
                TotalSessions = _dbContext.Classes.Count(c => c.TutorID == t.TutorID)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (raw == null)
            return null;

        var reviews = await _dbContext.Reviews
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
            DegreeVerified = raw.IsVerified,
            TranscriptVerified = raw.IsVerified,
            VideoUrl = raw.IntroVideoUrl,
            Rating = raw.AverageRating,
            TotalReviews = raw.TotalReviews,
            TotalSessions = raw.TotalSessions,
            TestPassRate = 0,
            HourlyRate = raw.HourlyRate,
            Availability = GroupAvailability(raw.AvailableSlots),
            JoinDate = raw.JoinDate.ToString("yyyy-MM-dd"),
            Location = raw.Location,
            TeachingStyle = raw.TeachingStyle,
            Achievements = raw.Achievements ?? new List<string>(),
            Role = raw.TutorType == TutorType.Teacher ? "teacher" : "tutor",
            YearsExperience = raw.YearsExperience,
            CurrentSchool = null,
            PlatformFeeRate = 0m,
            Reviews = reviews
        };
    }

    private static List<AvailabilityDayDto> GroupAvailability(List<AvailableSlot>? slots)
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

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
                TotalSessions = _dbContext.ClassSessions.Count(c => c.TutorID == t.TutorID),
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
}

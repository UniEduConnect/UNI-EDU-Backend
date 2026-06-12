using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.TutorPosts;

// Tutor posts a "looking for students" ad.
public class CreateTutorPostRequest
{
    public Guid SubjectId { get; set; }
    public string? GradeLevels { get; set; }
    public int? HourlyRate { get; set; }
    public string? PreferredSchedule { get; set; }
    public string? Note { get; set; }
}

public class CreateTutorPostValidator : AbstractValidator<CreateTutorPostRequest>
{
    public CreateTutorPostValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty().WithMessage("Vui lòng chọn môn học.");
    }
}

public class TutorPostResponse
{
    public Guid Id { get; set; }
    public Guid TutorId { get; set; }
    public string TutorName { get; set; } = string.Empty;
    public string? TutorAvatar { get; set; }
    public float Rating { get; set; }
    public Guid SubjectId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? GradeLevels { get; set; }
    public int? HourlyRate { get; set; }
    public string? PreferredSchedule { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "open";
    public DateTime CreatedAt { get; set; }
}

public class TutorPostListQuery
{
    public string? Search { get; set; }
    public string? Subject { get; set; }   // subject name
    public int Page { get; set; } = 1;
}

// Tutor accepts a student's application after passing an AI test (>=80%) for the post subject.
public class AcceptApplicationRequest
{
    public Guid AiTestAttemptId { get; set; }
}

public class TutorPostApplicationResponse
{
    public Guid Id { get; set; }
    public Guid TutorPostId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
}

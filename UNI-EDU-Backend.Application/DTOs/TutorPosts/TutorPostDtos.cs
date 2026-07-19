using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.TutorPosts;

// Tutor posts a "looking for students" ad.
public class CreateTutorPostRequest
{
    public Guid SubjectId { get; set; }
    public string? GradeLevels { get; set; }
    public int? HourlyRate { get; set; }
    public string? PreferredSchedule { get; set; }
    // Minimum teaching commitment in months (>= 1).
    public int? DurationMonths { get; set; }
    public string? Note { get; set; }
}

public class CreateTutorPostValidator : AbstractValidator<CreateTutorPostRequest>
{
    public CreateTutorPostValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty().WithMessage("Vui lòng chọn môn học.");
        RuleFor(x => x.DurationMonths)
            .NotNull().WithMessage("Vui lòng nhập thời lượng dạy.")
            .GreaterThanOrEqualTo(1).WithMessage("Thời lượng dạy tối thiểu là 1 tháng.");
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
    public int? DurationMonths { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "open";
    public DateTime CreatedAt { get; set; }

    // True when the caller (a Student) has a pending "đăng ký học" application on this post.
    // Always false for Parent callers, since applications are keyed by StudentId.
    public bool HasPendingApplication { get; set; }
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

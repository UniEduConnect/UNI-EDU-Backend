using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.ClassRequests;

// Student posts a "looking for a tutor" request.
public class CreateClassRequestRequest
{
    public Guid SubjectId { get; set; }
    public int Grade { get; set; }
    public string? PreferredSchedule { get; set; }
    public int? Budget { get; set; }
    public string? Note { get; set; }

    // Only used when a Parent posts on behalf of a linked child; ignored for Student callers.
    public Guid? StudentId { get; set; }
}

public class CreateClassRequestValidator : AbstractValidator<CreateClassRequestRequest>
{
    public CreateClassRequestValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty().WithMessage("Vui lòng chọn môn học.");
        RuleFor(x => x.Grade).InclusiveBetween(1, 12).WithMessage("Lớp không hợp lệ.");
    }
}

// Tutor accepts a request — must pass an AI test (>= 80%) for the subject first.
public class AcceptClassRequestRequest
{
    public Guid AiTestAttemptId { get; set; }
}

public class ClassRequestResponse
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string School { get; set; } = string.Empty;
    public int Grade { get; set; }
    public Guid SubjectId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? PreferredSchedule { get; set; }
    public int? Budget { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "open";
    public string? AssignedTutorName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ClassRequestListQuery
{
    public string? Search { get; set; }
    public string? Subject { get; set; }   // subject name
    public int Page { get; set; } = 1;
}

namespace UNI_EDU_Backend.Application.DTOs.Trials;

public class TrialResponse
{
    public Guid Id { get; set; }

    public Guid TutorId { get; set; }
    public string TutorName { get; set; } = string.Empty;

    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;

    public Guid? ParentId { get; set; }

    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; }

    public string? Goals { get; set; }
    public string? CurrentLevel { get; set; }
    public string? Note { get; set; }

    // Serialized as enum name (e.g. "Pending"). Frontend treats as a closed set.
    public string Status { get; set; } = string.Empty;

    // Null until the tutor accepts or rejects.
    public DateTime? ReviewedAt { get; set; }

    // Tutor's note from the accept/reject transition (typically a rejection reason).
    public string? ReviewNote { get; set; }

    // Null until the student/parent marks the trial Completed.
    public DateTime? CompletedAt { get; set; }

    // Student/parent's review of the completed trial session.
    public string? Feedback { get; set; }
    public double? Rating { get; set; }

    public DateTime CreatedAt { get; set; }
}

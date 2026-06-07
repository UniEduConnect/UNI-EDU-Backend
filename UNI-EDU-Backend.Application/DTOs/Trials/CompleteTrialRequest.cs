namespace UNI_EDU_Backend.Application.DTOs.Trials;

public class CompleteTrialRequest
{
    // Student/parent's free-text review of the trial session.
    public string? Feedback { get; set; }

    // 1.0 - 5.0 stars when provided (supports half-stars). Null = no rating submitted.
    public double? Rating { get; set; }
}

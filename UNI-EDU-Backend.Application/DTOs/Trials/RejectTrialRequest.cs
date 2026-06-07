namespace UNI_EDU_Backend.Application.DTOs.Trials;

public class RejectTrialRequest
{
    // Optional rejection reason. Stored as ReviewNote on TrialBooking and surfaced to the
    // requester (student/parent) so they understand why the trial was declined.
    public string? ReviewNote { get; set; }
}

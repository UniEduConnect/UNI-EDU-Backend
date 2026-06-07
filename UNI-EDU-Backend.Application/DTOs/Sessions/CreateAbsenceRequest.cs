namespace UNI_EDU_Backend.Application.DTOs.Sessions;

public class CreateAbsenceRequest
{
    public string Reason { get; set; } = string.Empty;

    // "tutor" | "student" — which side is reporting the absence.
    public string RequestedBy { get; set; } = string.Empty;
}

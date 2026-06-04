namespace UNI_EDU_Backend.Application.DTOs.Profile;

// A session in the caller's personal schedule, enriched with class + counterpart info.
public class ScheduleItemResponse
{
    public Guid SessionId { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;

    // The other party from the caller's perspective (tutor name for students/parents, student name for tutors).
    public string CounterpartName { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }

    // session status / format as lowercase wire strings
    public string Status { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
}

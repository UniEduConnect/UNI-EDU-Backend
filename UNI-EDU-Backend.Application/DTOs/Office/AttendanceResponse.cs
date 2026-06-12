namespace UNI_EDU_Backend.Application.DTOs.Office;

// Office monitoring view of a session (one row per session).
public class AttendanceResponse
{
    public Guid Id { get; set; } // SessionID
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Tutor { get; set; } = string.Empty;
    public string Student { get; set; } = string.Empty;

    public string Date { get; set; } = string.Empty; // yyyy-MM-dd
    public string Time { get; set; } = string.Empty; // HH:mm

    // "upcoming" | "pending" | "completed" | "reported"
    public string Status { get; set; } = string.Empty;

    // True once the student/parent confirmed the session (escrow-releasing confirm).
    public bool ParentConfirmed { get; set; }

    // True once the office acknowledged attendance.
    public bool OfficeConfirmed { get; set; }
}

namespace UNI_EDU_Backend.Application.DTOs.Reviews;

// A review written by the current student/parent (their perspective: who they reviewed).
public class MyReviewResponse
{
    public int Id { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public Guid TutorId { get; set; }
    public string TutorName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

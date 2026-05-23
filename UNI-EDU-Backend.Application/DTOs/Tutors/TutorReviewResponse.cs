namespace UNI_EDU_Backend.Application.DTOs.Tutors;

public class TutorReviewResponse
{
    public int Id { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = default!;
    public string StudentName { get; set; } = default!;
    public string ParentName { get; set; } = default!;
    public int Rating { get; set; }
    public string Comment { get; set; } = default!;
    public string Date { get; set; } = default!;
    public string Avatar { get; set; } = default!;
    public string Subject { get; set; } = default!;
}

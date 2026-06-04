namespace UNI_EDU_Backend.Application.DTOs.Reviews;

public class ReviewResponse
{
    public int Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid TutorId { get; set; }
    public Guid ReviewerId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

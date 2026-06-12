namespace UNI_EDU_Backend.Application.DTOs.Reviews;

public class CreateReviewRequest
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

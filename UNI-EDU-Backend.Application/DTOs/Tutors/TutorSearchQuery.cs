namespace UNI_EDU_Backend.Application.DTOs.Tutors;

public class TutorSearchQuery
{
    public string? Search { get; set; }
    public string? Subject { get; set; }
    public string Type { get; set; } = "all";
    public int MinPrice { get; set; } = 0;
    public int MaxPrice { get; set; } = 500_000;
    public int Page { get; set; } = 1;
}

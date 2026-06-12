namespace UNI_EDU_Backend.Application.DTOs.Stats;

// Aggregate platform metrics for the public landing page.
public class PublicStatsResponse
{
    public int Tutors { get; set; }
    public int Students { get; set; }
    public int Classes { get; set; }
    public int SessionsCompleted { get; set; }
    public int SatisfactionPct { get; set; } // avg review rating / 5 * 100
}

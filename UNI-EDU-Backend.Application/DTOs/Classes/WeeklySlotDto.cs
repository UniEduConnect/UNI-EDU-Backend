namespace UNI_EDU_Backend.Application.DTOs.Classes;

public class WeeklySlotDto
{
    /// <summary>0=Sunday, 1=Monday, ..., 6=Saturday (matches .NET DayOfWeek).</summary>
    public int DayOfWeek { get; set; }

    /// <summary>Format "HH:mm" or "HH:mm:ss".</summary>
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

namespace UNI_EDU_Backend.Application.DTOs.Tutors;

public class AvailabilityDayDto
{
    public string Day { get; set; } = default!;
    public List<string> Slots { get; set; } = new();
}

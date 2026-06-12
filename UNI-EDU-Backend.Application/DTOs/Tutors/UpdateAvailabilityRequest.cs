namespace UNI_EDU_Backend.Application.DTOs.Tutors;

// Full replace of the tutor's weekly availability slots.
// Each slot is { Day, Time } e.g. { "Thứ 2", "18:00-20:00" }.
public class UpdateAvailabilityRequest
{
    public List<AvailableSlotDto> Slots { get; set; } = [];
}

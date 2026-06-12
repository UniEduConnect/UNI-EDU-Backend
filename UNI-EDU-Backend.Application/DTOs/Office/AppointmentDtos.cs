namespace UNI_EDU_Backend.Application.DTOs.Office;

public class AppointmentResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? WithName { get; set; }
    public Guid? WithUserId { get; set; }
    public DateTime ScheduledAt { get; set; }

    // "scheduled" | "completed" | "cancelled"
    public string Status { get; set; } = "scheduled";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SaveAppointmentRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? WithName { get; set; }
    public Guid? WithUserId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string? Notes { get; set; }
}

public class AppointmentListQuery
{
    // "scheduled" | "completed" | "cancelled". Omit for all.
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
}

// Heuristic schedule suggestion (not ML). Echoes a buildable plan from the constraints.
public class AiScheduleRequest
{
    public string? Subject { get; set; }
    public List<string>? PreferredDays { get; set; }
    public int SessionsPerWeek { get; set; } = 2;
    public string? PreferredTime { get; set; } // e.g. "18:00-20:00"
    public string? Note { get; set; }
}

public class AiScheduleResponse
{
    public string Message { get; set; } = string.Empty;
    public List<AvailableSlotDto> SuggestedSlots { get; set; } = [];
}

public class AvailableSlotDto
{
    public string Day { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
}

namespace UNI_EDU_Backend.Application.DTOs.Office;

public class RoomAvailabilityDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Building { get; set; }
    // Display allocation snapshot. The authoritative figure is Summary.Free below.
    public bool IsOccupied { get; set; }
}

public class RoomInventorySummary
{
    public int Total { get; set; }
    public int Free { get; set; }
    public int Occupied { get; set; }      // # offline/hybrid classes needing a room in the window (real demand)
    public double OccupancyRate { get; set; }
}

public class RoomInventoryResponse
{
    public List<RoomAvailabilityDto> Rooms { get; set; } = new();
    public RoomInventorySummary Summary { get; set; } = new();
}

namespace UNI_EDU_Backend.Application.DTOs.Trials;

public class TrialListQuery
{
    // Optional. One of: pending | accepted | rejected | cancelled (case-insensitive).
    // Omit to return every status.
    public string? Status { get; set; }

    // 1-based. Server page size is fixed at 10.
    public int Page { get; set; } = 1;
}

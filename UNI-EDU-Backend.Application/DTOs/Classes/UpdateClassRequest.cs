namespace UNI_EDU_Backend.Application.DTOs.Classes;

// Admin-only partial update. Only non-null fields are applied.
// Deliberately narrow: money/escrow, schedule, session counts, and tutor/student/subject
// links are NOT patchable here because they are coupled to the wallet ledger and the
// generated Session rows. completed/cancelled transitions belong to the escrow settlement
// flow, not this endpoint.
public class UpdateClassRequest
{
    public string? Name { get; set; }

    // One of: searching | active | paused (case-insensitive).
    public string? Status { get; set; }
}

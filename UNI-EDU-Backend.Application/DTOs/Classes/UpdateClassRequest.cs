namespace UNI_EDU_Backend.Application.DTOs.Classes;

// Admin partial update. Only non-null fields are applied.
// NOTE: fee/escrow are decoupled here — changing Fee updates the tuition figure but does
// NOT re-run the wallet escrow ledger; admins are responsible for that.
public class UpdateClassRequest
{
    public string? Name { get; set; }

    // One of: searching | active | paused | completed | cancelled (case-insensitive).
    public string? Status { get; set; }

    public Guid? SubjectId { get; set; }
    public Guid? TutorId { get; set; }
    public Guid? StudentId { get; set; }

    // online | offline | hybrid
    public string? Format { get; set; }

    public decimal? Fee { get; set; }
    public int? TotalSessions { get; set; }
}

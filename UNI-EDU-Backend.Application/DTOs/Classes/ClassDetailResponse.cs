namespace UNI_EDU_Backend.Application.DTOs.Classes;

/// <summary>
/// Superset class shape for <c>GET /api/classes/{id}</c>. All roles consume this DTO
/// and pick the fields they need on the client. Backend handles authorization
/// (Tutor/Student only see their own class; Admin/Parent-of-student see all).
/// </summary>
public class ClassDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // IDs (kept for admin views + client-side joins)
    public Guid TutorId { get; set; }
    public Guid StudentId { get; set; }
    public Guid SubjectId { get; set; }

    // Denormalized display fields (saves a roundtrip on the client)
    public string Subject { get; set; } = string.Empty;
    public string TutorName { get; set; } = string.Empty;
    public string TutorAvatar { get; set; } = string.Empty;
    public string TutorType { get; set; } = string.Empty;        // "tutor" | "teacher"
    public string StudentName { get; set; } = string.Empty;
    public string StudentAvatar { get; set; } = string.Empty;
    public string ParentName { get; set; } = string.Empty;

    // Class state
    public string Format { get; set; } = string.Empty;
    public decimal Fee { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<WeeklySlotDto> WeeklySlots { get; set; } = new();
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }

    // Escrow (tutor + admin views)
    public decimal EscrowAmount { get; set; }
    public decimal EscrowReleased { get; set; }
    public string EscrowStatus { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime StartDate { get; set; }

    // Embedded children
    public List<SessionResponse> Sessions { get; set; } = new();
    public List<MaterialResponse> Materials { get; set; } = new();
}

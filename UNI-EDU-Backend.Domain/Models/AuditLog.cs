using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UNI_EDU_Backend.Domain.Models
{
    // Append-only record of privileged admin/finance actions for the audit-log screen.
    public class AuditLog
    {
        [Key]
        public Guid AuditLogID { get; set; }

        // The acting admin (nullable so the row survives if the user is later deleted).
        [ForeignKey("Actor")]
        public Guid? ActorUserId { get; set; }
        public string ActorName { get; set; } = string.Empty;

        // Short verb phrase, e.g. "Duyệt tài khoản", "Từ chối rút tiền".
        public string Action { get; set; } = string.Empty;

        // Human-readable description of what was acted on, e.g. "Nguyễn Văn An (Gia sư)".
        public string Target { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public virtual User? Actor { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace UNI_EDU_Backend.Domain.Models
{
    // A parent's request to link to a student account. The student must approve
    // before Student.ParentID is set (consent-based linking).
    public class ParentChildLinkRequest
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ParentID { get; set; }
        public Guid StudentID { get; set; }

        // "pending" | "approved" | "rejected"
        public string Status { get; set; } = "pending";

        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        // Navigation
        public virtual Parent Parent { get; set; }
        public virtual Student Student { get; set; }
    }
}

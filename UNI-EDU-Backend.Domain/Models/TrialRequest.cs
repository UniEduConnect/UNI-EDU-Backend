using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Domain.Models
{
    // A student's request for a trial lesson with a tutor.
    public class TrialRequest
    {
        [Key]
        public Guid TrialRequestID { get; set; }

        [ForeignKey("Student")]
        public Guid StudentID { get; set; }

        [ForeignKey("Tutor")]
        public Guid TutorID { get; set; }

        [ForeignKey("Subject")]
        public Guid? SubjectID { get; set; }

        // Requested slot, free-text to match the tutor's availability shape (e.g. "Thứ 2", "18:00-20:00").
        public string Day { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;

        public string? Message { get; set; }

        public TrialStatus Status { get; set; } = TrialStatus.Pending;
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        public virtual Student Student { get; set; } = null!;
        public virtual Tutor Tutor { get; set; } = null!;
        public virtual Subject? Subject { get; set; }
    }
}

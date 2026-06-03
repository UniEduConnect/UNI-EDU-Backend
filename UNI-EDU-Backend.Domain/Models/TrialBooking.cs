using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Domain.Models
{
    public class TrialBooking
    {
        [Key]
        public Guid TrialID { get; set; }

        [ForeignKey("Tutor")]
        public Guid TutorID { get; set; }

        [ForeignKey("Student")]
        public Guid StudentID { get; set; }

        // Null when a Student books for themselves; set to the Parent's id when a Parent books.
        [ForeignKey("Parent")]
        public Guid? ParentID { get; set; }

        [ForeignKey("Subject")]
        public Guid SubjectID { get; set; }

        public DateTime RequestedAt { get; set; }

        public string? Goals { get; set; }
        public string? CurrentLevel { get; set; }
        public string? Note { get; set; }

        public TrialStatus Status { get; set; }

        // Stamped when tutor accepts/rejects. Null while Status == Pending.
        public DateTime? ReviewedAt { get; set; }

        // Tutor-supplied note attached to the review (typically the rejection reason).
        public string? ReviewNote { get; set; }

        // Stamped when student/parent marks the trial Completed. Null until that happens.
        public DateTime? CompletedAt { get; set; }

        // Student/parent-supplied review of the trial session.
        public string? Feedback { get; set; }
        public double? Rating { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public virtual Tutor Tutor { get; set; }
        public virtual Student Student { get; set; }
        public virtual Parent? Parent { get; set; }
        public virtual Subject Subject { get; set; }
    }
}

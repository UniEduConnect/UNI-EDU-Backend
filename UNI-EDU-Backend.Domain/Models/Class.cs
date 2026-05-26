using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Domain.Models
{
    public class Class
    {
        [Key]
        public Guid ClassID { get; set; }

        [ForeignKey("Tutor")]
        public Guid TutorID { get; set; }

        [ForeignKey("Student")]
        public Guid StudentID { get; set; }

        [ForeignKey("Subject")]
        public Guid SubjectID { get; set; }

        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public decimal TuitionFee { get; set; }
        public ClassStatus Status { get; set; }
        public ClassFormat Format { get; set; }
        public List<ClassScheduleSlot> WeeklySlots { get; set; } = new();
        public int TotalSessions { get; set; }
        public int CompletedSessions { get; set; }

        public decimal EscrowAmount { get; set; }
        public decimal EscrowReleased { get; set; }
        public EscrowStatus EscrowStatus { get; set; }
        public int ReleaseMilestone { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public virtual Tutor Tutor { get; set; }
        public virtual Student Student { get; set; }
        public virtual Subject Subject { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
        public virtual ICollection<ClassMaterial> Materials { get; set; } = new List<ClassMaterial>();
    }
}

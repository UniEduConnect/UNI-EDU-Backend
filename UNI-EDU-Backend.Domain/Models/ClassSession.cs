using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Domain.Models
{
    public class ClassSession
    {
        [Key]
        public Guid ClassID { get; set; }

        [ForeignKey("Tutor")]
        public Guid TutorID { get; set; }

        [ForeignKey("Student")]
        public Guid StudentID { get; set; }

        [ForeignKey("Subject")]
        public Guid SubjectID { get; set; }

        public DateTime StartDate { get; set; }
        public decimal TuitionFee { get; set; }
        public ClassStatus Status { get; set; }

        // Navigation Properties
        public virtual Tutor Tutor { get; set; }

        public virtual Student Student { get; set; }
        public virtual Subject Subject { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
    }
}
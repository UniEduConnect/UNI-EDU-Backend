using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UNI_EDU_Backend.Domain.Models
{
    public class Review
    {
        [Key]
        public int ReviewID { get; set; }

        [ForeignKey("Reviewer")]
        public Guid ReviewerID { get; set; }

        [ForeignKey("Tutor")]
        public Guid TutorID { get; set; }

        [ForeignKey("ClassSession")]
        public Guid ClassID { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string Comment { get; set; }
        public DateTime ReviewDate { get; set; }

        // Navigation Properties
        public virtual User Reviewer { get; set; }

        public virtual Tutor Tutor { get; set; }
        public virtual ClassSession ClassSession { get; set; }
    }
}
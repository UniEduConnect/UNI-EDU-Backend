using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UNI_EDU_Backend.Domain.Models
{
    public class Tutor
    {
        [Key]
        [ForeignKey("User")]
        public Guid TutorID { get; set; }

        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public string Degree { get; set; }
        public string? Experience { get; set; }
        public string? StudentIdNumber { get; set; }
        public float AverageRating { get; set; }

        // Navigation Properties
        public virtual User User { get; set; }

        public virtual ICollection<ClassSession> Classes { get; set; }
    }
}
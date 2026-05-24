using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Domain.Models
{
    public class Tutor
    {
        [Key]
        [ForeignKey("User")]
        public Guid TutorID { get; set; }

        public string FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Degree { get; set; }
        public string? Bio { get; set; }
        public string? StudentIdNumber { get; set; }
        public float? AverageRating { get; set; }

        public string? AvatarUrl { get; set; }
        public string? Location { get; set; }
        public string? School { get; set; }
        public int? HourlyRate { get; set; }
        public int? YearsExperience { get; set; }
        public bool? IsVerified { get; set; }
        public string? TeachingStyle { get; set; }
        public string? IntroVideoUrl { get; set; }
        public TutorType? TutorType { get; set; }

        public List<string>? Certificates { get; set; } = new();
        public List<string>? Achievements { get; set; } = new();
        public List<AvailableSlot>? AvailableSlots { get; set; } = new();

        // Navigation Properties
        public virtual User User { get; set; }

        public virtual ICollection<ClassSession> Classes { get; set; }
        public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();
        public virtual ICollection<TutorSubject> TutorSubjects { get; set; } = new List<TutorSubject>();
    }
}
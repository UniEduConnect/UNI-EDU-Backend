using System.ComponentModel.DataAnnotations;

namespace UNI_EDU_Backend.Domain.Models
{
    // A tutor's "looking for students" post (job board, mirror of ClassRequest).
    // Students browse these and can reach out / view the tutor's profile.
    public class TutorPost
    {
        [Key]
        public Guid Id { get; set; }

        public Guid TutorId { get; set; }
        public Guid SubjectId { get; set; }
        public string? GradeLevels { get; set; }      // e.g. "Lớp 10-12"
        public int? HourlyRate { get; set; }
        public string? PreferredSchedule { get; set; }
        // Minimum teaching commitment in months (>= 1), mirroring ClassRequest.DurationMonths.
        public int? DurationMonths { get; set; }
        public string? Note { get; set; }

        // "open" | "closed"
        public string Status { get; set; } = "open";

        public DateTime CreatedAt { get; set; }

        public virtual Tutor Tutor { get; set; }
        public virtual Subject Subject { get; set; }
    }
}

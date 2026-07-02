using System.ComponentModel.DataAnnotations;

namespace UNI_EDU_Backend.Domain.Models
{
    // A student's open "looking for a tutor" post (job board). A tutor browses these
    // and accepts one — but only after passing a tutor-test for that acceptance.
    public class ClassRequest
    {
        [Key]
        public Guid Id { get; set; }

        public Guid StudentId { get; set; }
        public Guid SubjectId { get; set; }
        public int Grade { get; set; }
        public string? PreferredSchedule { get; set; }
        public int? Budget { get; set; }
        // Minimum learning commitment in months (>= 3). Nullable for rows created before this field.
        public int? DurationMonths { get; set; }
        public string? Note { get; set; }

        // "open" | "assigned" | "cancelled"
        public string Status { get; set; } = "open";

        public Guid? AssignedTutorId { get; set; }
        // The tutor-test submission used to accept this request (enforces one test per acceptance).
        public int? AcceptedSubmissionId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? AssignedAt { get; set; }

        public virtual Student Student { get; set; }
        public virtual Subject Subject { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace UNI_EDU_Backend.Domain.Models
{
    // An AI-generated qualification test a tutor takes to accept a class/student.
    // Questions + correct answers are stored server-side (jsonb) so grading is server-authoritative.
    public class AiTestAttempt
    {
        [Key]
        public Guid Id { get; set; }

        public Guid TutorId { get; set; }
        public Guid SubjectId { get; set; }

        // JSON array: [{ "content": "...", "options": ["..."], "correctIndex": 0 }]
        public string QuestionsJson { get; set; } = "[]";

        public int? Score { get; set; }        // percent 0-100
        public bool Passed { get; set; }
        public bool Used { get; set; }         // consumed by an acceptance (one test per acceptance)

        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }

        public virtual Subject Subject { get; set; }
    }
}

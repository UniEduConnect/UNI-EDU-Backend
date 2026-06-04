using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UNI_EDU_Backend.Domain.Models
{
    public class Submission
    {
        [Key]
        public int SubmissionID { get; set; }

        [ForeignKey("Exam")]
        public int ExamID { get; set; }

        [ForeignKey("User")]
        public Guid UserID { get; set; }

        public DateTime SubmissionDate { get; set; }

        public string Answers { get; set; }

        public float Score { get; set; }

        // Auto-grading summary captured at submit time so attempt history needs no re-grading.
        public int CorrectCount { get; set; }
        public int TotalQuestions { get; set; }

        public string? AIFeedback { get; set; }

        // Navigation Properties
        public virtual Exam Exam { get; set; }

        public virtual User User { get; set; }
    }
}
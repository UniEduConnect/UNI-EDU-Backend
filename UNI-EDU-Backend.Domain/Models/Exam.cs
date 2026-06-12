using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Domain.Models
{
    public class Exam
    {
        [Key]
        public int ExamID { get; set; }

        [ForeignKey("Subject")]
        public Guid SubjectID { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; } // In minutes
        public ExamType Type { get; set; }
        public CreatorType CreatedBy { get; set; }

        // Lifecycle / visibility for the exam-manager portal.
        public ExamStatus Status { get; set; } = ExamStatus.Draft;
        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;

        // Pricing + scheduling shown on the exam-manager and student-tests screens.
        public decimal Fee { get; set; }
        public int Year { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Attempt limits + scoring config.
        public int MaxAttemptsPerUser { get; set; } = 1;
        public int ScoreScale { get; set; } = 10; // 10 or 100
        public bool AiProctoring { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("CreatorTutor")]
        public Guid? CreatorTutorID { get; set; } // Null if created by AI or Admin

        // Navigation Properties
        public virtual Subject Subject { get; set; }

        public virtual Tutor CreatorTutor { get; set; }
        public virtual ICollection<ExamQuestion> ExamQuestions { get; set; }
        public virtual ICollection<Submission> Submissions { get; set; }
    }
}
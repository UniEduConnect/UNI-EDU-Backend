using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Domain.Models
{
    public class Question
    {
        [Key]
        public int QuestionID { get; set; }

        [ForeignKey("Subject")]
        public Guid SubjectID { get; set; }

        public string Content { get; set; }
        public QuestionType Type { get; set; }
        public DifficultyLevel Difficulty { get; set; }
        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }
        public string CorrectAnswer { get; set; }

        // Optional curriculum metadata surfaced in the exam-manager question bank.
        public string? Topic { get; set; }
        public string? Standard { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual Subject Subject { get; set; }
        public virtual ICollection<ExamQuestion> ExamQuestions { get; set; }
    }
}
using System.ComponentModel.DataAnnotations.Schema;

namespace UNI_EDU_Backend.Domain.Models
{
    public class ExamQuestion
    {
        [ForeignKey("Exam")]
        public int ExamID { get; set; }

        [ForeignKey("Question")]
        public int QuestionID { get; set; }

        public int QuestionOrder { get; set; }

        public virtual Exam Exam { get; set; }
        public virtual Question Question { get; set; }
    }
}
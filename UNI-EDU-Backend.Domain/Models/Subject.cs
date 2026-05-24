using System.ComponentModel.DataAnnotations;

namespace UNI_EDU_Backend.Domain.Models
{
    public class Subject
    {
        [Key]
        public Guid SubjectID { get; set; }

        public string SubjectName { get; set; }

        public virtual ICollection<Class> Classes { get; set; }
        public virtual ICollection<Question> Questions { get; set; }
        public virtual ICollection<Exam> Exams { get; set; }
        public virtual ICollection<Tutor> Tutors { get; set; } = new List<Tutor>();
    }
}
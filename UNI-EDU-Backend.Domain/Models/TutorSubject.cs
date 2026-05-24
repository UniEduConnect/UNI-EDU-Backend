namespace UNI_EDU_Backend.Domain.Models
{
    public class TutorSubject
    {
        public Guid TutorID { get; set; }
        public Guid SubjectID { get; set; }

        public virtual Tutor Tutor { get; set; }
        public virtual Subject Subject { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace UNI_EDU_Backend.Domain.Models
{
    // A student "đăng ký học" on a tutor's post. The tutor must pass an AI test (>=80%)
    // for the post's subject to accept it.
    public class TutorPostApplication
    {
        [Key]
        public Guid Id { get; set; }

        public Guid TutorPostId { get; set; }
        public Guid StudentId { get; set; }
        public Guid TutorId { get; set; }     // denormalized from the post
        public Guid SubjectId { get; set; }   // denormalized from the post (AI test subject)

        // "pending" | "accepted" | "rejected"
        public string Status { get; set; } = "pending";

        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        public virtual TutorPost TutorPost { get; set; }
        public virtual Student Student { get; set; }
    }
}

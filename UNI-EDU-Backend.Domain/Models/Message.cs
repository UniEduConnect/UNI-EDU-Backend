using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UNI_EDU_Backend.Domain.Models
{
    // A chat message within a class thread (student ↔ tutor ↔ parent).
    public class Message
    {
        [Key]
        public Guid MessageID { get; set; }

        [ForeignKey("Class")]
        public Guid ClassID { get; set; }

        [ForeignKey("Sender")]
        public Guid SenderID { get; set; }

        // "student" | "tutor" | "parent" — the sender's role at send time.
        public string SenderRole { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Class Class { get; set; } = null!;
        public virtual User Sender { get; set; } = null!;
    }
}

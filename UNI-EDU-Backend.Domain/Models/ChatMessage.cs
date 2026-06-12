using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UNI_EDU_Backend.Domain.Models
{
    // A message in a class chat (tutor ↔ student ↔ parent), scoped to a class.
    public class ChatMessage
    {
        [Key]
        public Guid MessageID { get; set; }

        [ForeignKey("Class")]
        public Guid ClassID { get; set; }

        [ForeignKey("Sender")]
        public Guid SenderID { get; set; }

        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }

        public virtual Class Class { get; set; }
        public virtual User Sender { get; set; }
    }
}

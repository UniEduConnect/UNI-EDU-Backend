using System.ComponentModel.DataAnnotations;

namespace UNI_EDU_Backend.Domain.Models
{
    // A direct message in a parent ↔ tutor conversation. The conversation is keyed by the
    // (ParentID, TutorID) pair; SenderID is whichever of the two authored this message.
    // Plain Guid columns (no navigation) keep this clear of multiple-cascade-path config.
    public class DmMessage
    {
        [Key]
        public Guid MessageID { get; set; }

        public Guid ParentID { get; set; }
        public Guid TutorID { get; set; }
        public Guid SenderID { get; set; }

        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}

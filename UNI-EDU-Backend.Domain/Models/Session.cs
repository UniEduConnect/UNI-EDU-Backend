using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Domain.Models
{
    public class Session
    {
        [Key]
        public Guid SessionID { get; set; }

        [ForeignKey("Class")]
        public Guid ClassID { get; set; }

        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public SessionStatus Status { get; set; }
        public ClassFormat Format { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Class Class { get; set; }
    }
}

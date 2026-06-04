using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UNI_EDU_Backend.Domain.Models
{
    public class Notification
    {
        [Key]
        public Guid NotificationID { get; set; }

        [ForeignKey("User")]
        public Guid UserID { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        // "info" | "warning" | "success" | "error"
        public string Type { get; set; } = "info";

        // Optional deep link the frontend can navigate to.
        public string? Link { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual User User { get; set; } = null!;
    }
}

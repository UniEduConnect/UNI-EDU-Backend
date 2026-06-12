using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Domain.Models
{
    // An office-scheduled appointment (interview, consultation, etc.).
    public class Appointment
    {
        [Key]
        public Guid AppointmentID { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Who the appointment is with (optional free reference).
        public string? WithName { get; set; }
        [ForeignKey("WithUser")]
        public Guid? WithUserId { get; set; }

        public DateTime ScheduledAt { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual User? WithUser { get; set; }
    }
}

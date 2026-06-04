using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Domain.Models
{
    // An issue raised against a class/session, tracked by the office portal.
    public class Incident
    {
        [Key]
        public Guid IncidentID { get; set; }

        [ForeignKey("Class")]
        public Guid ClassID { get; set; }

        // Optional: the specific session the incident relates to.
        [ForeignKey("Session")]
        public Guid? SessionID { get; set; }

        // Who reported it (nullable so the row survives if the user is deleted).
        [ForeignKey("Reporter")]
        public Guid? ReporterUserId { get; set; }
        public string ReporterName { get; set; } = string.Empty;
        public string ReporterRole { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public IncidentPriority Priority { get; set; } = IncidentPriority.Medium;
        public IncidentStatus Status { get; set; } = IncidentStatus.Pending;

        public string? Resolution { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public virtual Class Class { get; set; } = null!;
        public virtual Session? Session { get; set; }
        public virtual User? Reporter { get; set; }
    }
}

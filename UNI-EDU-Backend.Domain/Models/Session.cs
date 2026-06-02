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

        // Lifecycle timestamps. StartedAt set on PATCH /start, EndedAt on PATCH /end.
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        // Lesson report captured by the tutor on PATCH /end.
        public string? Content { get; set; }
        public string? Notes { get; set; }
        public string? Homework { get; set; }
        public List<string>? HomeworkFiles { get; set; } = new();

        // Student rating captured on PATCH /rate. Also mirrored into the tutor's review aggregate.
        public int? Rating { get; set; }
        public string? RatingComment { get; set; }

        // Absence flow: a request is opened by one party (tutor/student) and approved by the
        // counter-party. AbsenceApproved is null while pending, true once the other side approves.
        public string? AbsenceReason { get; set; }
        public string? AbsenceRequestedBy { get; set; }
        public bool? AbsenceApproved { get; set; }

        public virtual Class Class { get; set; }
    }
}

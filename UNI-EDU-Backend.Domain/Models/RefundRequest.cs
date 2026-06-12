using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Domain.Models
{
    // A request to refund (part of) a class's held escrow back to the student's wallet.
    public class RefundRequest
    {
        [Key]
        public Guid RefundRequestID { get; set; }

        [ForeignKey("Class")]
        public Guid ClassID { get; set; }

        // Who raised it (the booking student or their parent).
        [ForeignKey("Requester")]
        public Guid? RequesterUserId { get; set; }

        public decimal Amount { get; set; }
        // Snapshot of the maximum refundable (remaining held escrow) at request time.
        public decimal MaxAmount { get; set; }
        public string Reason { get; set; } = string.Empty;

        public RefundStatus Status { get; set; } = RefundStatus.Pending;
        public DateTime CreatedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }
        [ForeignKey("Reviewer")]
        public Guid? ReviewerID { get; set; }
        public string? ReviewNote { get; set; }

        public virtual Class Class { get; set; } = null!;
        public virtual User? Requester { get; set; }
        public virtual User? Reviewer { get; set; }
    }
}

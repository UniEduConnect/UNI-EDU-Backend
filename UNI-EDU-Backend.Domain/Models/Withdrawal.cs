using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Domain.Models
{
    public class Withdrawal
    {
        [Key]
        public Guid WithdrawalID { get; set; }

        // FK to Tutor (TutorID == UserID).
        [ForeignKey("Tutor")]
        public Guid TutorID { get; set; }

        public decimal Amount { get; set; }

        // Currently always "bank" — momo/vnpay payouts require provider-specific integration.
        public string Method { get; set; } = string.Empty;

        // Bank info — required when Method == "bank".
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }

        // Optional note from the tutor about the request.
        public string? Note { get; set; }

        public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Pending;
        public DateTime RequestedAt { get; set; }

        // Filled by the future finance approve/reject endpoint.
        public DateTime? ReviewedAt { get; set; }

        [ForeignKey("Reviewer")]
        public Guid? ReviewerID { get; set; }

        // Free-text reason/comment from the reviewer (especially for rejections).
        public string? ReviewNote { get; set; }

        // Ledger row mirroring this withdrawal so it appears in the tutor's transaction
        // history (Pending on request → Completed on approve / Failed on reject).
        public Guid? LedgerTransactionId { get; set; }

        public virtual Tutor Tutor { get; set; } = null!;
        public virtual User? Reviewer { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Domain.Models
{
    public class WalletTransaction
    {
        [Key]
        public Guid TransactionID { get; set; }

        [ForeignKey("Wallet")]
        public Guid UserID { get; set; }

        public WalletTxType Type { get; set; }
        public decimal Amount { get; set; }

        // Pending until a payment provider confirms (deposits/withdrawals). Ledger entries
        // written by internal flows (escrow, refunds) are created already Completed.
        public WalletTxStatus Status { get; set; } = WalletTxStatus.Completed;

        // Payment-provider metadata (deposits/withdrawals). Null for internal ledger entries.
        public string? Method { get; set; }       // "momo" | "vnpay" | "bank"
        public string? ProviderRef { get; set; }  // our order id sent to the provider (momo orderId)
        public string? ProviderTxnId { get; set; } // provider's own transaction id from the callback

        [ForeignKey("Class")]
        public Guid? RelatedClassID { get; set; }

        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public virtual Wallet Wallet { get; set; }
        public virtual Class Class { get; set; }
    }
}

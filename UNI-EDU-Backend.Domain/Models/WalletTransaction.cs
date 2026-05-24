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

        [ForeignKey("Class")]
        public Guid? RelatedClassID { get; set; }

        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public virtual Wallet Wallet { get; set; }
        public virtual Class Class { get; set; }
    }
}

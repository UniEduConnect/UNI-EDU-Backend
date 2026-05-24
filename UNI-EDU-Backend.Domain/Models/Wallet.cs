using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UNI_EDU_Backend.Domain.Models
{
    public class Wallet
    {
        [Key]
        [ForeignKey("User")]
        public Guid UserID { get; set; }

        public decimal Balance { get; set; }
        public decimal EscrowBalance { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<WalletTransaction> Transactions { get; set; }
    }
}

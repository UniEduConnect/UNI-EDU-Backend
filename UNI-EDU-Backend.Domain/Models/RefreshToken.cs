using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UNI_EDU_Backend.Domain.Models
{
    public class RefreshToken
    {
        [Key]
        public Guid TokenID { get; set; }

        [ForeignKey("User")]
        public Guid UserID { get; set; }

        public string Token { get; set; }
        public string JwtId { get; set; }
        public bool IsUsed { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        // Navigation Property
        public virtual User User { get; set; }
    }
}
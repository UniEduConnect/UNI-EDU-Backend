using System.ComponentModel.DataAnnotations;

namespace UNI_EDU_Backend.Domain.Models
{
    // A one-time email verification code. The code is stored hashed (never plaintext).
    public class EmailOtp
    {
        [Key]
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;
        public string CodeHash { get; set; } = string.Empty;  // SHA-256 of the 6-digit code
        public string Purpose { get; set; } = "register";

        public DateTime ExpiresAt { get; set; }
        public int Attempts { get; set; }
        public bool Consumed { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

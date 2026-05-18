using System.ComponentModel.DataAnnotations;
using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Domain.Models;

public class User
{
    [Key]
    public Guid UserID { get; set; }
    public string Fullname { get; set; }
    public string HashedPassword { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public virtual Tutor Tutor { get; set; }
    public virtual Parent Parent { get; set; }
    public virtual Student Student { get; set; }
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; }
    public virtual ICollection<Submission> Submissions { get; set; }
}

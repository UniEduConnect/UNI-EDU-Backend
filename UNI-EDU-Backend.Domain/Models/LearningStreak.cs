using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UNI_EDU_Backend.Domain.Models
{
    // One row per user. Tracks a daily "study streak": the number of consecutive
    // calendar days (Vietnam time) the user has had learning activity.
    // PK == UserID (1-1 with User, shared primary key).
    public class LearningStreak
    {
        [Key]
        [ForeignKey("User")]
        public Guid UserID { get; set; }

        // Length of the current consecutive run, in days.
        public int CurrentStreak { get; set; }

        // Best run ever achieved (kept as an achievement even after a break).
        public int LongestStreak { get; set; }

        // Last calendar day (Asia/Ho_Chi_Minh) the user was active. Null = never checked in.
        public DateOnly? LastActivityDate { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation Property
        public virtual User User { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace UNI_EDU_Backend.Domain.Models
{
    // Single-row platform configuration for the admin settings screen.
    public class SystemSetting
    {
        [Key]
        public Guid SettingID { get; set; }

        public string PlatformName { get; set; } = "UNI-EDU";
        public int EscrowPercent { get; set; } = 100;
        public int EscrowHoldDays { get; set; } = 3;

        public bool EnableExams { get; set; } = true;
        public bool EnableChat { get; set; } = true;
        public bool EnablePayments { get; set; } = true;
        public bool MaintenanceMode { get; set; }

        public bool EmailNotifications { get; set; } = true;
        public bool SmsNotifications { get; set; }
        public bool PushNotifications { get; set; } = true;
        public bool TwoFactorAuth { get; set; }

        public int SessionTimeout { get; set; } = 60;   // minutes
        public int MaxLoginAttempts { get; set; } = 5;

        public DateTime UpdatedAt { get; set; }
    }
}

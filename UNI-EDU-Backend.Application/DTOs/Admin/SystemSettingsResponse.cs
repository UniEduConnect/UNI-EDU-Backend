namespace UNI_EDU_Backend.Application.DTOs.Admin;

public class SystemSettingsResponse
{
    public string PlatformName { get; set; } = string.Empty;
    public int EscrowPercent { get; set; }
    public int EscrowHoldDays { get; set; }
    public bool EnableExams { get; set; }
    public bool EnableChat { get; set; }
    public bool EnablePayments { get; set; }
    public bool MaintenanceMode { get; set; }
    public bool EmailNotifications { get; set; }
    public bool SmsNotifications { get; set; }
    public bool PushNotifications { get; set; }
    public bool TwoFactorAuth { get; set; }
    public int SessionTimeout { get; set; }
    public int MaxLoginAttempts { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Full replace of editable settings. All fields required (the admin form sends the whole object).
public class UpdateSystemSettingsRequest
{
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
    public int SessionTimeout { get; set; } = 60;
    public int MaxLoginAttempts { get; set; } = 5;
}

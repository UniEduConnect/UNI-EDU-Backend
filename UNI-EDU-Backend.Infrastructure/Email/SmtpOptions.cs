namespace UNI_EDU_Backend.Infrastructure.Email;

// Bound from the "Smtp" config section. Put the password in .env as Smtp__Password.
public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "UNI EDU";
    public bool UseStartTls { get; set; } = true;
}

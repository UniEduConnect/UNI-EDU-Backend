namespace UNI_EDU_Backend.Application.Interfaces;

public interface IEmailSender
{
    // Returns true if the email was dispatched; false if SMTP is not configured or sending failed
    // (callers can fall back, e.g. log the code in Development).
    Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody, string textBody, CancellationToken cancellationToken);
}

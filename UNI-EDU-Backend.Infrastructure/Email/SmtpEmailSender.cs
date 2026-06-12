using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using UNI_EDU_Backend.Application.Interfaces;

namespace UNI_EDU_Backend.Infrastructure.Email;

// SMTP email sender (MailKit). Degrades gracefully to false when SMTP is unconfigured.
public class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _o = options.Value;
    private readonly ILogger<SmtpEmailSender> _logger = logger;

    public async Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody, string textBody, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_o.Host) || string.IsNullOrWhiteSpace(_o.FromEmail))
        {
            _logger.LogWarning("SMTP not configured (Smtp:Host / Smtp:FromEmail) — email to {To} was not sent.", toEmail);
            return false;
        }

        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_o.FromName, _o.FromEmail));
            msg.To.Add(new MailboxAddress(toName ?? string.Empty, toEmail));
            msg.Subject = subject;
            msg.Body = new BodyBuilder { HtmlBody = htmlBody, TextBody = textBody }.ToMessageBody();

            using var client = new SmtpClient();
            var secure = _o.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;
            await client.ConnectAsync(_o.Host, _o.Port, secure, cancellationToken);
            if (!string.IsNullOrWhiteSpace(_o.User))
                await client.AuthenticateAsync(_o.User, _o.Password, cancellationToken);
            await client.SendAsync(msg, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}.", toEmail);
            return false;
        }
    }
}

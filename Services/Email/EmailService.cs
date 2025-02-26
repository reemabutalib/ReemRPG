using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Support", _emailSettings.SmtpUsername));
        message.To.Add(new MailboxAddress("Receiver", toEmail));
        message.Subject = subject;

        message.Body = new TextPart("plain") { Text = body };

        using (var client = new SmtpClient())
        {
            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);

            // 🔒 Fetch SMTP password securely from an environment variable
            var smtpPassword = Environment.GetEnvironmentVariable("SmtpPassword") ?? _emailSettings.SmtpPassword;

            await client.AuthenticateAsync(_emailSettings.SmtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}

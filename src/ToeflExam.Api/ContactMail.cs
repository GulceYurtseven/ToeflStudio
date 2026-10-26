using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ToeflExam.Api;

/// <summary>SMTP delivery settings; credentials are loaded from secrets or environment variables.</summary>
public sealed class ContactMailOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "";
    public string Recipient { get; set; } = "gulceyurtseven@gmail.com";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From) && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
}

/// <summary>Separates SMTP transport from form validation and allows deterministic delivery tests.</summary>
public interface IContactMailSender
{
    Task SendAsync(string subject, string message, string replyTo, CancellationToken ct);
}

/// <summary>Delivers plain-text feedback to a fixed administrator address over mandatory TLS.</summary>
public sealed class SmtpContactMailSender(IOptions<ContactMailOptions> options) : IContactMailSender
{
    /// <summary>Builds a MIME message without user-controlled recipients, sender headers or HTML.</summary>
    public async Task SendAsync(string subject, string message, string replyTo, CancellationToken ct)
    {
        var settings = options.Value;
        using var mail = new MimeMessage();
        mail.From.Add(MailboxAddress.Parse(settings.From));
        mail.To.Add(MailboxAddress.Parse(settings.Recipient));
        mail.ReplyTo.Add(MailboxAddress.Parse(replyTo));
        mail.Subject = "[TOEFL Studio] " + subject;
        mail.Body = new TextPart("plain") { Text = "Contact / feedback from: " + replyTo + "\n\n" + message };
        using var client = new SmtpClient { Timeout = 15_000 };
        // Never downgrade to cleartext or bypass the server's certificate validation.
        await client.ConnectAsync(settings.Host, settings.Port, settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(settings.Username, settings.Password, ct);
        await client.SendAsync(mail, ct);
        await client.DisconnectAsync(true, ct);
    }
}

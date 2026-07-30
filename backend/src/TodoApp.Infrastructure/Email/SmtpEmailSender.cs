using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using TodoApp.Application.Common;

namespace TodoApp.Infrastructure.Email;

/// <summary>
/// Real SMTP sending via MailKit (Microsoft recommends against the older,
/// deprecated System.Net.Mail.SmtpClient for new code — this is the modern
/// replacement). Works with any standard SMTP provider (SendGrid, Mailgun,
/// Amazon SES's SMTP interface, Gmail's SMTP relay, a company mail server, etc.) —
/// just fill in the Smtp:* settings, no code change needed per-provider.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;

    public SmtpEmailSender(IOptions<SmtpSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
            throw new InvalidOperationException("SMTP is not configured (Smtp:Host is empty) — check IEmailSettingsProvider.IsConfigured before calling this.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();

        // StartTlsWhenAvailable covers the common case (port 587) without
        // needing a separate "use SSL" toggle per provider; SmtpsOnConnect
        // (implicit TLS, port 465) also works with the same call since
        // MailKit auto-negotiates based on what the server offers.
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);

        if (!string.IsNullOrEmpty(_settings.Username))
            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

namespace TodoApp.Application.Common;

/// <summary>
/// Abstraction so Application doesn't depend on a specific mail library
/// (implemented in Infrastructure via SMTP/MailKit). Callers should check
/// IsConfigured first — see IEmailSettingsProvider — since local dev often
/// runs without any real SMTP server set up.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

/// <summary>Lets Application code check "is real email sending set up at all?" without depending on the concrete SMTP settings type (which lives in Infrastructure).</summary>
public interface IEmailSettingsProvider
{
    bool IsConfigured { get; }

    /// <summary>Base URL of the frontend, used to build links inside emails (e.g. "https://app.example.com").</summary>
    string FrontendBaseUrl { get; }
}

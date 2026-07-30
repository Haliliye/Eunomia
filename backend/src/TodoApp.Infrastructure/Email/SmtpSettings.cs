namespace TodoApp.Infrastructure.Email;

public class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Eunomia";

    /// <summary>Where email links (verify/reset) point — the frontend's public URL.</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    /// <summary>No Host configured means "no real SMTP set up" — callers fall back to the dev-mode token-in-response behavior instead of trying (and failing) to send.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}

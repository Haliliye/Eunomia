namespace TodoApp.Infrastructure.Email;

/// <summary>
/// Brevo's transactional email HTTP API (api.brevo.com) — the alternative to
/// SMTP that actually works on Render's free tier. Render blocks outbound
/// traffic to SMTP ports (25/465/587) on free web services (see
/// render.com/changelog, effective Sept 2025), but ordinary HTTPS (443) is
/// never blocked, so switching from SMTP to Brevo's REST API sidesteps that
/// entirely — no paid plan needed.
/// </summary>
public class BrevoApiSettings
{
    public const string SectionName = "BrevoApi";

    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Eunomia";

    /// <summary>Where email links (verify/reset) point — the frontend's public URL.</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    /// <summary>No ApiKey configured means "not set up" — callers fall back to the SMTP settings (or dev-mode) instead.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}

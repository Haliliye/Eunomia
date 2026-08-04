using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TodoApp.Application.Common;

namespace TodoApp.Infrastructure.Email;

/// <summary>
/// Sends via Brevo's transactional email HTTP API instead of SMTP — see
/// BrevoApiSettings for why. Registered as a typed HttpClient
/// (services.AddHttpClient&lt;IEmailSender, BrevoApiEmailSender&gt;() in
/// DependencyInjection), so the HttpClient here is pooled/managed by
/// IHttpClientFactory rather than constructed directly.
/// </summary>
public class BrevoApiEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly BrevoApiSettings _settings;
    private readonly ILogger<BrevoApiEmailSender> _logger;

    public BrevoApiEmailSender(HttpClient httpClient, IOptions<BrevoApiSettings> settings, ILogger<BrevoApiEmailSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.brevo.com/v3/");
        _httpClient.DefaultRequestHeaders.Add("api-key", _settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
            throw new InvalidOperationException("Brevo API is not configured (BrevoApi:ApiKey is empty) — check IEmailSettingsProvider.IsConfigured before calling this.");

        var payload = new
        {
            sender = new { name = _settings.FromName, email = _settings.FromEmail },
            to = new[] { new { email = toEmail } },
            subject,
            htmlContent = htmlBody,
        };

        var response = await _httpClient.PostAsJsonAsync("smtp/email", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Surfaced in full — Brevo's error body usually names the exact
            // problem (unverified sender, bad API key, etc.), which is worth
            // having in the logs rather than just "it failed".
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Brevo API returned {StatusCode}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Brevo API request failed with {response.StatusCode}: {body}");
        }
    }
}

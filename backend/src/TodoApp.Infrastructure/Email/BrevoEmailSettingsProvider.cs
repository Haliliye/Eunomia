using Microsoft.Extensions.Options;
using TodoApp.Application.Common;

namespace TodoApp.Infrastructure.Email;

public class BrevoEmailSettingsProvider : IEmailSettingsProvider
{
    private readonly BrevoApiSettings _settings;

    public BrevoEmailSettingsProvider(IOptions<BrevoApiSettings> settings)
    {
        _settings = settings.Value;
    }

    public bool IsConfigured => _settings.IsConfigured;
    public string FrontendBaseUrl => _settings.FrontendBaseUrl;
}

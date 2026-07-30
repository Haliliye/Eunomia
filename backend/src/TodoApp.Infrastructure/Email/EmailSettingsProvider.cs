using Microsoft.Extensions.Options;
using TodoApp.Application.Common;

namespace TodoApp.Infrastructure.Email;

public class EmailSettingsProvider : IEmailSettingsProvider
{
    private readonly SmtpSettings _settings;

    public EmailSettingsProvider(IOptions<SmtpSettings> settings)
    {
        _settings = settings.Value;
    }

    public bool IsConfigured => _settings.IsConfigured;
    public string FrontendBaseUrl => _settings.FrontendBaseUrl;
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TodoApp.IntegrationTests.Fixtures;

/// <summary>
/// Points the real app's configuration at a shared Testcontainers Mongo
/// instance (own database name per factory instance, so tests using
/// different instances never collide) instead of whatever's in
/// appsettings.json, and supplies a Jwt secret + writable attachment
/// storage path so the app can actually start under test. Extracted from
/// AuthFlowTests (where it originated) so any test class driving the real
/// HTTP pipeline via WebApplicationFactory&lt;Program&gt; can reuse it
/// instead of redefining its own copy.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _mongoConnectionString;
    private readonly string _databaseNamePrefix;
    private readonly string _attachmentStorageRoot = Path.Combine(Path.GetTempPath(), $"todoapp-attachments-test-{Guid.NewGuid():N}");

    public ApiFactory(string mongoConnectionString, string databaseNamePrefix = "todoapp_api_tests")
    {
        _mongoConnectionString = mongoConnectionString;
        _databaseNamePrefix = databaseNamePrefix;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = _mongoConnectionString,
                ["MongoDb:DatabaseName"] = $"{_databaseNamePrefix}_{Guid.NewGuid():N}",
                ["Jwt:SecretKey"] = "integration-test-secret-key-at-least-32-chars-long",
                ["Jwt:Issuer"] = "TodoApp",
                ["Jwt:Audience"] = "TodoAppClient",
                ["Jwt:ExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenExpiryDays"] = "30",
                ["AttachmentStorage:RootPath"] = _attachmentStorageRoot,
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (Directory.Exists(_attachmentStorageRoot))
            Directory.Delete(_attachmentStorageRoot, recursive: true);
    }
}

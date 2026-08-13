using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TodoApp.IntegrationTests.Fixtures;
using Xunit;

namespace TodoApp.IntegrationTests;

/// <summary>
/// Drives the real ASP.NET Core pipeline end to end via WebApplicationFactory
/// (not a mocked handler) — this is the one place that would have caught a
/// bug purely in routing/middleware/cookie wiring, since every other test in
/// this solution either calls a handler directly (unit tests) or a repository
/// directly (the other integration tests). Uses the same Testcontainers Mongo
/// instance as the rest of this collection, under its own database name so
/// its data can't collide with anything else running against that container.
///
/// Cookies are extracted from Set-Cookie and resent manually (via
/// ExtractCookieHeader/WithCookie below) rather than relying on HttpClient's
/// default cookie-jar behavior, which isn't guaranteed the same way a real
/// browser's is — this keeps the test correct regardless of that.
/// </summary>
[Collection("Mongo collection")]
// Not IClassFixture<Factory> — xUnit would try to construct Factory itself
// then, and it has no way to supply the mongoConnectionString constructor
// argument. Factory is built manually below instead, once MongoFixture's
// connection string is available.
public class AuthFlowTests : IDisposable
{
    private readonly ApiFactory _factory;

    public AuthFlowTests(MongoFixture mongoFixture)
    {
        _factory = new ApiFactory(mongoFixture.ConnectionString, "todoapp_authflow_tests");
    }

    public void Dispose() => _factory.Dispose();

    private static string ExtractCookieHeader(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return string.Empty;
        return string.Join("; ", values.Select(v => v.Split(';')[0]));
    }

    private static HttpRequestMessage WithCookie(HttpMethod method, string url, string cookieHeader)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(cookieHeader))
            request.Headers.Add("Cookie", cookieHeader);
        return request;
    }

    [Fact]
    public async Task Register_SetsHttpOnlyCookies_AndNeverReturnsTheRawTokenInTheBody()
    {
        // Arrange
        var client = _factory.CreateClient();
        var email = $"cookie-test-{Guid.NewGuid():N}@example.com";

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName = "Cookie Test",
            password = "StrongPass123!",
        });

        // Assert
        response.EnsureSuccessStatusCode();

        var setCookieHeaders = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToList() : new List<string>();
        Assert.Contains(setCookieHeaders, h => h.StartsWith("access_token=") && h.Contains("HttpOnly"));
        Assert.Contains(setCookieHeaders, h => h.StartsWith("refresh_token=") && h.Contains("HttpOnly"));

        // The whole point of this migration: the JSON body must NOT contain
        // the raw token anywhere, even though the cookie header does.
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"token\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"refreshToken\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WeakPassword_IsRejected()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"weak-{Guid.NewGuid():N}@example.com",
            displayName = "Weak Password",
            password = "alllowercase", // no uppercase, no digit, no symbol
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedRequest_Succeeds_UsingOnlyTheCookieFromRegistration_NoAuthorizationHeader()
    {
        // Arrange
        var client = _factory.CreateClient();
        var email = $"authed-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName = "Authed User",
            password = "StrongPass123!",
        });
        registerResponse.EnsureSuccessStatusCode();
        var cookie = ExtractCookieHeader(registerResponse);

        // Act — deliberately no Authorization header anywhere, only the cookie.
        var meRequest = WithCookie(HttpMethod.Get, "/api/users/me/notification-preferences", cookie);
        var meResponse = await client.SendAsync(meRequest);

        // Assert
        meResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Logout_ClearsCookies_AndSubsequentRequestIsUnauthorized()
    {
        var client = _factory.CreateClient();
        var email = $"logout-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName = "Logout User",
            password = "StrongPass123!",
        });
        registerResponse.EnsureSuccessStatusCode();
        var cookie = ExtractCookieHeader(registerResponse);

        var logoutRequest = WithCookie(HttpMethod.Post, "/api/auth/logout", cookie);
        var logoutResponse = await client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        // Re-sending the SAME (now server-revoked) cookie should no longer work.
        var afterLogoutRequest = WithCookie(HttpMethod.Get, "/api/users/me/notification-preferences", cookie);
        var afterLogout = await client.SendAsync(afterLogoutRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task WithoutAnyAuth_ProtectedEndpoint_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/users/me/notification-preferences");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

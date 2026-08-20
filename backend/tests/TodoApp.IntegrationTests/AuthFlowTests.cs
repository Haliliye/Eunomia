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

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += needle.Length;
        }
        return count;
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

        // Checked against the combined raw header text rather than
        // requiring each cookie to be its own exact entry in
        // TryGetValues — HttpClient's header parsing for repeated
        // Set-Cookie headers isn't always split the same way across
        // handlers/.NET versions (a comma inside an Expires date, e.g.
        // "Expires=Wed, 21 Oct 2026...", can confuse naive comma-based
        // splitting), so this only relies on the substrings actually
        // being present somewhere in what the server sent.
        var setCookieHeaders = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToList() : new List<string>();
        var combinedCookieText = string.Join("\n", setCookieHeaders);
        Assert.Contains("access_token=", combinedCookieText);
        Assert.Contains("refresh_token=", combinedCookieText);
        // Both cookies are HttpOnly (see AuthController.SetAuthCookies) —
        // checking the substring appears at least twice (once per cookie)
        // rather than pinning down exactly which header it's attached to.
        Assert.True(CountOccurrences(combinedCookieText, "HttpOnly") >= 2,
            $"Expected both cookies to be HttpOnly. Raw Set-Cookie headers: {combinedCookieText}");

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
    public async Task Logout_ClearsCookiesAndRevokesTheRefreshToken()
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

        // The server instructs the client to delete both cookies (an expired
        // Set-Cookie for each) — this is the actual "clears cookies" the
        // test name refers to, checked directly rather than by re-sending
        // the old cookie and hoping the server rejects it: access tokens are
        // short-lived, stateless JWTs by design (see JwtSettings), so the
        // one issued at register is still cryptographically valid for its
        // remaining lifetime even after logout — only the refresh token is
        // actually server-revoked (checked below). Re-sending the old
        // access token and expecting 401 doesn't test anything real about
        // this design; it was asserting a property the system was never
        // meant to have.
        var setCookieHeaders = logoutResponse.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToList() : new List<string>();
        Assert.Contains(setCookieHeaders, h => h.StartsWith("access_token=") && h.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(setCookieHeaders, h => h.StartsWith("refresh_token=") && h.Contains("expires=", StringComparison.OrdinalIgnoreCase));

        // What logout actually revokes server-side: trying to use the
        // now-logged-out refresh token to mint a fresh access token must fail.
        var refreshRequest = WithCookie(HttpMethod.Post, "/api/auth/refresh", cookie);
        var refreshResponse = await client.SendAsync(refreshRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task WithoutAnyAuth_ProtectedEndpoint_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/users/me/notification-preferences");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

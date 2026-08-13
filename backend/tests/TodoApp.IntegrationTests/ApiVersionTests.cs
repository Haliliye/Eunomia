using System.Net.Http.Json;
using System.Linq;
using TodoApp.IntegrationTests.Fixtures;
using Xunit;

namespace TodoApp.IntegrationTests;

[Collection("Mongo collection")]
public class ApiVersionTests : IDisposable
{
    private readonly ApiFactory _factory;

    public ApiVersionTests(MongoFixture mongoFixture)
    {
        _factory = new ApiFactory(mongoFixture.ConnectionString, "todoapp_apiversion_tests");
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task AnyResponse_CarriesTheXApiVersionHeader()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.True(response.Headers.TryGetValues("X-Api-Version", out var values));
        Assert.Equal("1.0", values!.Single());
    }

    [Fact]
    public async Task GetApiVersion_ReturnsTheCurrentVersion()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/version");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<VersionResponse>();
        Assert.Equal("1.0", body!.Version);
    }

    private record VersionResponse(string Version);
}

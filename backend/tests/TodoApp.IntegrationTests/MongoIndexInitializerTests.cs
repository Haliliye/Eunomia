using MongoDB.Driver;
using TodoApp.IntegrationTests.Fixtures;
using Xunit;

namespace TodoApp.IntegrationTests;

[Collection("Mongo collection")]
public class MongoIndexInitializerTests
{
    private readonly MongoFixture _fixture;

    public MongoIndexInitializerTests(MongoFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EnsureIndexesAsync_CreatesExpectedIndexesOnUserStories()
    {
        var indexDocs = await (await _fixture.Context.UserStories.Indexes.ListAsync()).ToListAsync();
        var names = indexDocs.Select(doc => doc["name"].AsString).ToList();

        Assert.Contains(names, n => n.Contains("TeamId"));
        Assert.Contains(names, n => n.Contains("AssigneeId"));
    }

    [Fact]
    public async Task EnsureIndexesAsync_CreatesUniqueIndexOnUserEmail()
    {
        var indexDocs = await (await _fixture.Context.Users.Indexes.ListAsync()).ToListAsync();
        var emailIndex = indexDocs.FirstOrDefault(doc => doc["name"].AsString.Contains("Email"));

        Assert.NotNull(emailIndex);
        Assert.True(emailIndex!["unique"].AsBoolean);
    }

    [Fact]
    public async Task EnsureIndexesAsync_CreatesTtlIndexOnRefreshTokens()
    {
        var indexDocs = await (await _fixture.Context.RefreshTokens.Indexes.ListAsync()).ToListAsync();
        var ttlIndex = indexDocs.FirstOrDefault(doc => doc.Contains("expireAfterSeconds"));

        Assert.NotNull(ttlIndex);
    }
}

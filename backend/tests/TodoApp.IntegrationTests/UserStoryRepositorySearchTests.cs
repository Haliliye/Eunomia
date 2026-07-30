using TodoApp.Domain.UserStories;
using TodoApp.Infrastructure.Persistence.Repositories;
using TodoApp.IntegrationTests.Fixtures;
using Xunit;

namespace TodoApp.IntegrationTests;

[Collection("Mongo collection")]
public class UserStoryRepositorySearchTests
{
    private readonly UserStoryRepository _repository;

    public UserStoryRepositorySearchTests(MongoFixture fixture)
    {
        _repository = new UserStoryRepository(fixture.Context);
    }

    [Fact]
    public async Task SearchAsync_KeywordMatchesTitleViaTextIndex()
    {
        // Validates that the real text index MongoIndexInitializer creates
        // actually backs Builders.Filter.Text() the way SearchAsync expects —
        // a mocked repository can't catch a missing/misconfigured text index.
        var teamId = Guid.NewGuid().ToString();
        var keyword = $"Zephyr{Guid.NewGuid():N}";
        var story = UserStory.Create(Guid.NewGuid().ToString(), teamId, $"Fix the {keyword} bug", "some description");
        await _repository.AddAsync(story);

        var (items, totalCount) = await _repository.SearchAsync(
            teamId, status: null, priority: null, assigneeId: null, keyword: keyword,
            page: 1, pageSize: 25);

        Assert.Equal(1, totalCount);
        Assert.Contains(items, s => s.Id == story.Id);
    }

    [Fact]
    public async Task SearchAsync_FiltersByStatusAndPriorityTogether()
    {
        var teamId = Guid.NewGuid().ToString();

        var matching = UserStory.Create(Guid.NewGuid().ToString(), teamId, "Matching story", null);
        matching.ChangePriority(UserStoryPriority.Critical);
        await _repository.AddAsync(matching);

        var nonMatching = UserStory.Create(Guid.NewGuid().ToString(), teamId, "Non-matching story", null);
        nonMatching.ChangePriority(UserStoryPriority.Low);
        await _repository.AddAsync(nonMatching);

        var (items, totalCount) = await _repository.SearchAsync(
            teamId, status: "ToDo", priority: "Critical", assigneeId: null, keyword: null,
            page: 1, pageSize: 25);

        Assert.Equal(1, totalCount);
        Assert.Equal(matching.Id, items.Single().Id);
    }

    [Fact]
    public async Task SearchAsync_KeywordDoesNotMatchUnrelatedStories()
    {
        var teamId = Guid.NewGuid().ToString();
        var story = UserStory.Create(Guid.NewGuid().ToString(), teamId, "Completely unrelated title", null);
        await _repository.AddAsync(story);

        var (items, totalCount) = await _repository.SearchAsync(
            teamId, status: null, priority: null, assigneeId: null, keyword: $"NoSuchWord{Guid.NewGuid():N}",
            page: 1, pageSize: 25);

        Assert.Equal(0, totalCount);
        Assert.Empty(items);
    }
}

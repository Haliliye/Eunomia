using TodoApp.Domain.Teams;
using TodoApp.Infrastructure.Persistence.Repositories;
using TodoApp.IntegrationTests.Fixtures;
using Xunit;

namespace TodoApp.IntegrationTests;

[Collection("Mongo collection")]
public class TeamRepositoryTests
{
    private readonly TeamRepository _repository;

    public TeamRepositoryTests(MongoFixture fixture)
    {
        _repository = new TeamRepository(fixture.Context);
    }

    [Fact]
    public async Task GetByMemberIdAsync_FindsTeamsContainingThatMember()
    {
        // This exercises the ElemMatch query on Members.UserId that originally
        // threw MongoDB.Driver.Linq.ExpressionNotSupportedException when the
        // aggregate's computed Members property was queried directly against
        // Mongo — that's exactly why TeamDocument (a plain mapping shape)
        // exists. A mocked repository can't catch a regression here; a real
        // MongoDB container can.
        var ownerId = Guid.NewGuid().ToString();
        var team = Team.Create(Guid.NewGuid().ToString(), $"Team {Guid.NewGuid()}", null, ownerId);
        await _repository.AddAsync(team);

        var found = await _repository.GetByMemberIdAsync(ownerId);

        Assert.Contains(found, t => t.Id == team.Id);
    }

    [Fact]
    public async Task ExistsWithNameForUserAsync_ReturnsTrueOnlyForMatchingOwnerAndName()
    {
        var ownerId = Guid.NewGuid().ToString();
        var name = $"Unique Team {Guid.NewGuid()}";
        var team = Team.Create(Guid.NewGuid().ToString(), name, null, ownerId);
        await _repository.AddAsync(team);

        Assert.True(await _repository.ExistsWithNameForUserAsync(name, ownerId));
        Assert.False(await _repository.ExistsWithNameForUserAsync(name, Guid.NewGuid().ToString()));
    }

    [Fact]
    public async Task SearchByMemberIdAsync_PaginatesCorrectly()
    {
        var userId = Guid.NewGuid().ToString();
        for (var i = 0; i < 5; i++)
        {
            var team = Team.Create(Guid.NewGuid().ToString(), $"Paged Team {i}-{Guid.NewGuid()}", null, userId);
            await _repository.AddAsync(team);
        }

        var (items, totalCount) = await _repository.SearchByMemberIdAsync(userId, page: 1, pageSize: 2);

        Assert.True(totalCount >= 5);
        Assert.Equal(2, items.Count);
    }
}

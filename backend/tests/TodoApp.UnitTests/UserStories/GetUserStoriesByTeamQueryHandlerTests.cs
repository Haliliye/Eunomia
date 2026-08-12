using Moq;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Queries.GetUserStoriesByTeam;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using Xunit;

namespace TodoApp.UnitTests.UserStories;

/// <summary>
/// Regression coverage for a real IDOR gap found in the 2026-08-11 security
/// review: this query previously had no membership check at all — any
/// authenticated user could list any team's full backlog just by knowing
/// (or guessing) its id. See GetUserStoriesByTeamQueryHandler.
/// </summary>
public class GetUserStoriesByTeamQueryHandlerTests
{
    [Fact]
    public async Task Handle_RequesterNotATeamMember_ThrowsInvalidOperationException()
    {
        // Arrange
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");

        var teamRepoMock = new Mock<ITeamRepository>();
        teamRepoMock.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        var storyRepoMock = new Mock<IUserStoryRepository>();

        var handler = new GetUserStoriesByTeamQueryHandler(storyRepoMock.Object, teamRepoMock.Object);
        var query = new GetUserStoriesByTeamQuery(team.Id, "some-stranger");

        // Act & Assert — Team.EnsureIsMember throws InvalidOperationException
        // for a non-member (see Team.cs), not UnauthorizedAccessException.
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(query, CancellationToken.None));
        storyRepoMock.Verify(r => r.SearchAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RequesterIsATeamMember_ReturnsStories()
    {
        // Arrange
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");

        var teamRepoMock = new Mock<ITeamRepository>();
        teamRepoMock.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        var storyRepoMock = new Mock<IUserStoryRepository>();
        storyRepoMock.Setup(r => r.SearchAsync(
            team.Id, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserStory>(), 0));

        var handler = new GetUserStoriesByTeamQueryHandler(storyRepoMock.Object, teamRepoMock.Object);
        var query = new GetUserStoriesByTeamQuery(team.Id, "owner-1");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }
}

using Moq;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Commands.UpdateUserStory;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using Xunit;

namespace TodoApp.UnitTests.UserStories;

public class UpdateUserStoryCommandHandlerTests
{
    private const string TeamId = "team-1";
    private const string MemberUserId = "member-1";

    private static UserStory MakeStory(string title = "Original title") =>
        UserStory.Create(Guid.NewGuid().ToString(), TeamId, title, "Original description");

    private static Mock<ITeamRepository> MakeTeamRepositoryMock()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Test Team", null, MemberUserId);
        var teamRepositoryMock = new Mock<ITeamRepository>();
        teamRepositoryMock.Setup(r => r.GetByIdAsync(TeamId, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        return teamRepositoryMock;
    }

    [Fact]
    public async Task Handle_WithMatchingVersion_UpdatesAndIncrementsVersion()
    {
        // Arrange
        var story = MakeStory();
        Assert.Equal(0, story.Version); // sanity check on the starting version

        var repositoryMock = new Mock<IUserStoryRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);
        repositoryMock
            .Setup(r => r.UpdateWithConcurrencyCheckAsync(It.IsAny<UserStory>(), 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var realtimeMock = new Mock<IRealtimeNotifier>();
        var handler = new UpdateUserStoryCommandHandler(repositoryMock.Object, MakeTeamRepositoryMock().Object, realtimeMock.Object);
        var command = new UpdateUserStoryCommand(story.Id, "New title", "New description", null, null, 0, MemberUserId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("New title", story.Title);
        Assert.Equal(1, story.Version);
    }

    [Fact]
    public async Task Handle_WithStaleVersion_ThrowsConcurrencyConflictException()
    {
        // Arrange — the caller's ExpectedVersion (0) no longer matches the
        // story's current version (1), meaning someone else already saved a change.
        var story = MakeStory();
        story.UpdateDetails("Someone else's edit", "changed already"); // bumps Version to 1

        var repositoryMock = new Mock<IUserStoryRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);

        var realtimeMock = new Mock<IRealtimeNotifier>();
        var handler = new UpdateUserStoryCommandHandler(repositoryMock.Object, MakeTeamRepositoryMock().Object, realtimeMock.Object);
        var command = new UpdateUserStoryCommand(story.Id, "My conflicting edit", null, null, null, 0, MemberUserId);

        // Act & Assert
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => handler.Handle(command, CancellationToken.None));
        repositoryMock.Verify(
            r => r.UpdateWithConcurrencyCheckAsync(It.IsAny<UserStory>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDatabaseWriteLosesTheRace_ThrowsConcurrencyConflictException()
    {
        // Arrange — the in-memory version check passes, but the database-level
        // check (UpdateWithConcurrencyCheckAsync) reports no document matched,
        // simulating another write landing in between.
        var story = MakeStory();

        var repositoryMock = new Mock<IUserStoryRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);
        repositoryMock
            .Setup(r => r.UpdateWithConcurrencyCheckAsync(It.IsAny<UserStory>(), 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var realtimeMock = new Mock<IRealtimeNotifier>();
        var handler = new UpdateUserStoryCommandHandler(repositoryMock.Object, MakeTeamRepositoryMock().Object, realtimeMock.Object);
        var command = new UpdateUserStoryCommand(story.Id, "New title", null, null, null, 0, MemberUserId);

        // Act & Assert
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotATeamMember_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var story = MakeStory();

        var repositoryMock = new Mock<IUserStoryRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);

        var realtimeMock = new Mock<IRealtimeNotifier>();
        var handler = new UpdateUserStoryCommandHandler(repositoryMock.Object, MakeTeamRepositoryMock().Object, realtimeMock.Object);
        var command = new UpdateUserStoryCommand(story.Id, "New title", null, null, null, 0, "some-stranger");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
    }
}

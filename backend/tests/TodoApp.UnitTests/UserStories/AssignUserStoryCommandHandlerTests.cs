using MediatR;
using Moq;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Commands.AssignUserStory;
using TodoApp.Domain.Activities;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using Xunit;

namespace TodoApp.UnitTests.UserStories;

public class AssignUserStoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_AssigningNonMember_ThrowsInvalidOperationException()
    {
        // Arrange
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        var story = UserStory.Create(Guid.NewGuid().ToString(), team.Id, "Some story", null);

        var storyRepoMock = new Mock<IUserStoryRepository>();
        storyRepoMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);
        var teamRepoMock = new Mock<ITeamRepository>();
        teamRepoMock.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        var mediatorMock = new Mock<IMediator>();

        var realtimeMock = new Mock<IRealtimeNotifier>();
        var activityMock = new Mock<IActivityRepository>();
        var handler = new AssignUserStoryCommandHandler(storyRepoMock.Object, teamRepoMock.Object, mediatorMock.Object, realtimeMock.Object, activityMock.Object);
        var command = new AssignUserStoryCommand(story.Id, "not-a-member", "owner-1");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        mediatorMock.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AssigningTeamMember_AssignsAndDispatchesDomainEvent()
    {
        // Arrange
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        var story = UserStory.Create(Guid.NewGuid().ToString(), team.Id, "Some story", null);

        var storyRepoMock = new Mock<IUserStoryRepository>();
        storyRepoMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);
        var teamRepoMock = new Mock<ITeamRepository>();
        teamRepoMock.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        var mediatorMock = new Mock<IMediator>();

        var realtimeMock = new Mock<IRealtimeNotifier>();
        var activityMock = new Mock<IActivityRepository>();
        var handler = new AssignUserStoryCommandHandler(storyRepoMock.Object, teamRepoMock.Object, mediatorMock.Object, realtimeMock.Object, activityMock.Object);
        var command = new AssignUserStoryCommand(story.Id, "owner-1", "owner-1"); // owner-1 is a member (the owner)

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("owner-1", story.AssigneeId);
        // UserStoryAssignedEvent should have been published (US-118 — see UserStoryAssignedEventHandler).
        mediatorMock.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotATeamMember_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        var story = UserStory.Create(Guid.NewGuid().ToString(), team.Id, "Some story", null);

        var storyRepoMock = new Mock<IUserStoryRepository>();
        storyRepoMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);
        var teamRepoMock = new Mock<ITeamRepository>();
        teamRepoMock.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        var mediatorMock = new Mock<IMediator>();

        var realtimeMock = new Mock<IRealtimeNotifier>();
        var activityMock = new Mock<IActivityRepository>();
        var handler = new AssignUserStoryCommandHandler(storyRepoMock.Object, teamRepoMock.Object, mediatorMock.Object, realtimeMock.Object, activityMock.Object);
        var command = new AssignUserStoryCommand(story.Id, "owner-1", "some-stranger");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
    }
}

using Moq;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Commands.ChangeStatus;
using MediatR;
using TodoApp.Domain.Activities;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using Xunit;

namespace TodoApp.UnitTests.UserStories;

public class ChangeUserStoryStatusCommandHandlerTests
{
    private const string TeamId = "team-1";
    private const string MemberUserId = "member-1";

    private static Mock<ITeamRepository> MakeTeamRepositoryMock()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Test Team", null, MemberUserId);
        var teamRepositoryMock = new Mock<ITeamRepository>();
        teamRepositoryMock.Setup(r => r.GetByIdAsync(TeamId, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        return teamRepositoryMock;
    }

    [Fact]
    public async Task Handle_WithValidTransition_UpdatesStatus()
    {
        // Arrange
        var story = UserStory.Create(Guid.NewGuid().ToString(), TeamId, "Some story", null);
        var repositoryMock = new Mock<IUserStoryRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);

        var realtimeMock = new Mock<IRealtimeNotifier>();
        var activityMock = new Mock<IActivityRepository>();
        var mediatorMock = new Mock<IMediator>();
        var handler = new ChangeUserStoryStatusCommandHandler(repositoryMock.Object, MakeTeamRepositoryMock().Object, realtimeMock.Object, activityMock.Object, mediatorMock.Object);
        var command = new ChangeUserStoryStatusCommand(story.Id, "Analyze", MemberUserId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Analyze", story.Status);
        repositoryMock.Verify(r => r.UpdateAsync(story, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonAdjacentTransition_Succeeds()
    {
        // Arrange — status changes are no longer restricted to a fixed
        // adjacency graph; a board card can be dragged straight from ToDo to
        // Done (see UserStory.ChangeStatus).
        var story = UserStory.Create(Guid.NewGuid().ToString(), TeamId, "Some story", null);
        var repositoryMock = new Mock<IUserStoryRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);

        var realtimeMock = new Mock<IRealtimeNotifier>();
        var activityMock = new Mock<IActivityRepository>();
        var mediatorMock = new Mock<IMediator>();
        var handler = new ChangeUserStoryStatusCommandHandler(repositoryMock.Object, MakeTeamRepositoryMock().Object, realtimeMock.Object, activityMock.Object, mediatorMock.Object);
        var command = new ChangeUserStoryStatusCommand(story.Id, "Done", MemberUserId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Done", story.Status);
        repositoryMock.Verify(r => r.UpdateAsync(story, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TestFailure_CanMoveToDebug()
    {
        // Arrange — a failed test sends work to Debug rather than back to ToDo.
        var story = UserStory.Create(Guid.NewGuid().ToString(), TeamId, "Some story", null);
        story.ChangeStatus("Analyze");
        story.ChangeStatus("Dev");
        story.ChangeStatus("Test");

        var repositoryMock = new Mock<IUserStoryRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);

        var realtimeMock = new Mock<IRealtimeNotifier>();
        var activityMock = new Mock<IActivityRepository>();
        var mediatorMock = new Mock<IMediator>();
        var handler = new ChangeUserStoryStatusCommandHandler(repositoryMock.Object, MakeTeamRepositoryMock().Object, realtimeMock.Object, activityMock.Object, mediatorMock.Object);
        var command = new ChangeUserStoryStatusCommand(story.Id, "Debug", MemberUserId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Debug", story.Status);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotATeamMember_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var story = UserStory.Create(Guid.NewGuid().ToString(), TeamId, "Some story", null);
        var repositoryMock = new Mock<IUserStoryRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);

        var realtimeMock = new Mock<IRealtimeNotifier>();
        var activityMock = new Mock<IActivityRepository>();
        var mediatorMock = new Mock<IMediator>();
        var handler = new ChangeUserStoryStatusCommandHandler(repositoryMock.Object, MakeTeamRepositoryMock().Object, realtimeMock.Object, activityMock.Object, mediatorMock.Object);
        var command = new ChangeUserStoryStatusCommand(story.Id, "Analyze", "some-stranger");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CompletingARecurringStory_CreatesTheNextOccurrence()
    {
        // Arrange — US-129: completing a recurring story should spawn a follow-up.
        var story = UserStory.Create(Guid.NewGuid().ToString(), TeamId, "Water the plants", null);
        story.SetDueDate(new DateTime(2026, 1, 1));
        story.SetRecurrence(RecurrenceFrequency.Daily, null);
        story.ChangeStatus("Analyze");
        story.ChangeStatus("Dev");
        story.ChangeStatus("Test");

        var repositoryMock = new Mock<IUserStoryRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);
        UserStory? createdOccurrence = null;
        repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<UserStory>(), It.IsAny<CancellationToken>()))
            .Callback<UserStory, CancellationToken>((s, _) => createdOccurrence = s)
            .Returns(Task.CompletedTask);

        var realtimeMock = new Mock<IRealtimeNotifier>();
        var activityMock = new Mock<IActivityRepository>();
        var mediatorMock = new Mock<IMediator>();
        var handler = new ChangeUserStoryStatusCommandHandler(repositoryMock.Object, MakeTeamRepositoryMock().Object, realtimeMock.Object, activityMock.Object, mediatorMock.Object);
        var command = new ChangeUserStoryStatusCommand(story.Id, "Done", MemberUserId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(createdOccurrence);
        Assert.Equal("Water the plants", createdOccurrence!.Title);
        Assert.Equal(new DateTime(2026, 1, 2), createdOccurrence.DueDate);
        Assert.Equal(RecurrenceFrequency.Daily, createdOccurrence.RecurrenceFrequency);
    }

    [Fact]
    public async Task Handle_CompletingANonRecurringStory_DoesNotCreateAnyOccurrence()
    {
        // Arrange
        var story = UserStory.Create(Guid.NewGuid().ToString(), TeamId, "One-off task", null);
        story.ChangeStatus("Analyze");
        story.ChangeStatus("Dev");
        story.ChangeStatus("Test");

        var repositoryMock = new Mock<IUserStoryRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);

        var realtimeMock = new Mock<IRealtimeNotifier>();
        var activityMock = new Mock<IActivityRepository>();
        var mediatorMock = new Mock<IMediator>();
        var handler = new ChangeUserStoryStatusCommandHandler(repositoryMock.Object, MakeTeamRepositoryMock().Object, realtimeMock.Object, activityMock.Object, mediatorMock.Object);
        var command = new ChangeUserStoryStatusCommand(story.Id, "Done", MemberUserId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        repositoryMock.Verify(r => r.AddAsync(It.IsAny<UserStory>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

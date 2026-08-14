using Moq;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Commands.SetDueDate;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using Xunit;

namespace TodoApp.UnitTests.UserStories;

public class SetUserStoryDueDateCommandHandlerTests
{
    [Fact]
    public async Task Handle_RequesterNotATeamMember_ThrowsUnauthorizedAccessException()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        var story = UserStory.Create(Guid.NewGuid().ToString(), team.Id, "Some story", null);

        var storyRepoMock = new Mock<IUserStoryRepository>();
        storyRepoMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);
        var teamRepoMock = new Mock<ITeamRepository>();
        teamRepoMock.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        var realtimeMock = new Mock<IRealtimeNotifier>();

        var handler = new SetUserStoryDueDateCommandHandler(storyRepoMock.Object, teamRepoMock.Object, realtimeMock.Object);
        var command = new SetUserStoryDueDateCommand(story.Id, DateTime.UtcNow.AddDays(7), "some-stranger");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
        storyRepoMock.Verify(r => r.UpdateAsync(It.IsAny<UserStory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TeamMember_SetsDueDate()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        var story = UserStory.Create(Guid.NewGuid().ToString(), team.Id, "Some story", null);
        var dueDate = DateTime.UtcNow.AddDays(7);

        var storyRepoMock = new Mock<IUserStoryRepository>();
        storyRepoMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);
        var teamRepoMock = new Mock<ITeamRepository>();
        teamRepoMock.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        var realtimeMock = new Mock<IRealtimeNotifier>();

        var handler = new SetUserStoryDueDateCommandHandler(storyRepoMock.Object, teamRepoMock.Object, realtimeMock.Object);
        var command = new SetUserStoryDueDateCommand(story.Id, dueDate, "owner-1");

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(dueDate, story.DueDate);
        storyRepoMock.Verify(r => r.UpdateAsync(story, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullDueDate_ClearsIt()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        var story = UserStory.Create(Guid.NewGuid().ToString(), team.Id, "Some story", null);
        story.SetDueDate(DateTime.UtcNow.AddDays(3));

        var storyRepoMock = new Mock<IUserStoryRepository>();
        storyRepoMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);
        var teamRepoMock = new Mock<ITeamRepository>();
        teamRepoMock.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        var realtimeMock = new Mock<IRealtimeNotifier>();

        var handler = new SetUserStoryDueDateCommandHandler(storyRepoMock.Object, teamRepoMock.Object, realtimeMock.Object);
        var command = new SetUserStoryDueDateCommand(story.Id, null, "owner-1");

        await handler.Handle(command, CancellationToken.None);

        Assert.Null(story.DueDate);
    }
}

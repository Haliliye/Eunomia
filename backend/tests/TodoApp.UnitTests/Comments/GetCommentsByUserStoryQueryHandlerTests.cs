using Moq;
using TodoApp.Application.Comments.Queries.GetCommentsByUserStory;
using TodoApp.Domain.Comments;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using Xunit;

namespace TodoApp.UnitTests.Comments;

/// <summary>
/// Regression coverage for a real IDOR gap found in the 2026-08-11 security
/// review: this query previously had no membership check at all — any
/// authenticated user could read any story's comments just by knowing
/// (or guessing) its id. See GetCommentsByUserStoryQueryHandler.
/// </summary>
public class GetCommentsByUserStoryQueryHandlerTests
{
    [Fact]
    public async Task Handle_RequesterNotATeamMember_ThrowsInvalidOperationException()
    {
        // Arrange
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        var story = UserStory.Create(Guid.NewGuid().ToString(), team.Id, "Some story", null);

        var teamRepoMock = new Mock<ITeamRepository>();
        teamRepoMock.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        var storyRepoMock = new Mock<IUserStoryRepository>();
        storyRepoMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);
        var commentRepoMock = new Mock<ICommentRepository>();

        var handler = new GetCommentsByUserStoryQueryHandler(commentRepoMock.Object, storyRepoMock.Object, teamRepoMock.Object);
        var query = new GetCommentsByUserStoryQuery(story.Id, "some-stranger");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(query, CancellationToken.None));
        commentRepoMock.Verify(r => r.GetByUserStoryIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RequesterIsATeamMember_ReturnsComments()
    {
        // Arrange
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        var story = UserStory.Create(Guid.NewGuid().ToString(), team.Id, "Some story", null);

        var teamRepoMock = new Mock<ITeamRepository>();
        teamRepoMock.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        var storyRepoMock = new Mock<IUserStoryRepository>();
        storyRepoMock.Setup(r => r.GetByIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(story);
        var commentRepoMock = new Mock<ICommentRepository>();
        commentRepoMock.Setup(r => r.GetByUserStoryIdAsync(story.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Comment>());

        var handler = new GetCommentsByUserStoryQueryHandler(commentRepoMock.Object, storyRepoMock.Object, teamRepoMock.Object);
        var query = new GetCommentsByUserStoryQuery(story.Id, "owner-1");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        commentRepoMock.Verify(r => r.GetByUserStoryIdAsync(story.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}

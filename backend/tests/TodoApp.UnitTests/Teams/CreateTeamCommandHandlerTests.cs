using MediatR;
using Moq;
using TodoApp.Application.Teams.Commands.CreateTeam;
using TodoApp.Domain.Teams;
using Xunit;

namespace TodoApp.UnitTests.Teams;

public class CreateTeamCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithUniqueTeamName_CreatesTeamAndReturnsDto()
    {
        // Arrange
        var repositoryMock = new Mock<ITeamRepository>();
        repositoryMock
            .Setup(r => r.ExistsWithNameForUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var mediatorMock = new Mock<IMediator>();

        var handler = new CreateTeamCommandHandler(repositoryMock.Object, mediatorMock.Object);
        var command = new CreateTeamCommand("Platform Team", "Owns core infra", "user-1");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Platform Team", result.Name);
        Assert.Single(result.Members);
        Assert.Equal("user-1", result.Members.First().UserId);
        Assert.Equal("Owner", result.Members.First().Role);
        repositoryMock.Verify(r => r.AddAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()), Times.Once);
        // TeamCreatedEvent should be dispatched even though nothing subscribes to it yet.
        mediatorMock.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateTeamName_ThrowsInvalidOperationException()
    {
        // Arrange
        var repositoryMock = new Mock<ITeamRepository>();
        repositoryMock
            .Setup(r => r.ExistsWithNameForUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mediatorMock = new Mock<IMediator>();

        var handler = new CreateTeamCommandHandler(repositoryMock.Object, mediatorMock.Object);
        var command = new CreateTeamCommand("Platform Team", null, "user-1");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }
}

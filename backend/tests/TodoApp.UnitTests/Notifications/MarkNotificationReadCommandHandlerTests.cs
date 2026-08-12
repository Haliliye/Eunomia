using Moq;
using TodoApp.Application.Notifications.Commands.MarkNotificationRead;
using TodoApp.Domain.Notifications;
using Xunit;

namespace TodoApp.UnitTests.Notifications;

/// <summary>
/// Regression coverage for a real IDOR gap found in the 2026-08-11 security
/// review: this command previously had no ownership check at all — any
/// authenticated user could mark any other user's notification as read just
/// by knowing (or guessing) its id. See MarkNotificationReadCommandHandler.
/// </summary>
public class MarkNotificationReadCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotificationBelongsToSomeoneElse_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var notification = Notification.Create(Guid.NewGuid().ToString(), "recipient-1", NotificationType.Assignment, "You were assigned a story", "story-1");

        var repoMock = new Mock<INotificationRepository>();
        repoMock.Setup(r => r.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>())).ReturnsAsync(notification);

        var handler = new MarkNotificationReadCommandHandler(repoMock.Object);
        var command = new MarkNotificationReadCommand(notification.Id, "some-stranger");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
        repoMock.Verify(r => r.UpdateAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task Handle_NotificationBelongsToRequester_MarksAsRead()
    {
        // Arrange
        var notification = Notification.Create(Guid.NewGuid().ToString(), "recipient-1", NotificationType.Assignment, "You were assigned a story", "story-1");

        var repoMock = new Mock<INotificationRepository>();
        repoMock.Setup(r => r.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>())).ReturnsAsync(notification);

        var handler = new MarkNotificationReadCommandHandler(repoMock.Object);
        var command = new MarkNotificationReadCommand(notification.Id, "recipient-1");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(notification.IsRead);
        repoMock.Verify(r => r.UpdateAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
    }
}

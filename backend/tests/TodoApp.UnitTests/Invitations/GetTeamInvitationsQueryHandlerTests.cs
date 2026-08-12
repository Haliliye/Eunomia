using Moq;
using TodoApp.Application.Invitations.Queries.GetTeamInvitations;
using TodoApp.Domain.Invitations;
using TodoApp.Domain.Teams;
using Xunit;

namespace TodoApp.UnitTests.Invitations;

/// <summary>
/// Regression coverage for a real IDOR gap found in the 2026-08-11 security
/// review: this query previously had no permission check at all — any
/// authenticated user could list a team's pending invitations (who's been
/// invited, by whom) just by knowing/guessing its id. See
/// GetTeamInvitationsQueryHandler.
/// </summary>
public class GetTeamInvitationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_RequesterIsAnOrdinaryMember_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        team.AddMember("member-1", "owner-1"); // an ordinary member, not owner/admin

        var teamRepoMock = new Mock<ITeamRepository>();
        teamRepoMock.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        var invitationRepoMock = new Mock<IInvitationRepository>();

        var handler = new GetTeamInvitationsQueryHandler(invitationRepoMock.Object, teamRepoMock.Object);
        var query = new GetTeamInvitationsQuery(team.Id, "member-1");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(query, CancellationToken.None));
        invitationRepoMock.Verify(r => r.GetPendingByTeamIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RequesterIsOwner_ReturnsInvitations()
    {
        // Arrange
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");

        var teamRepoMock = new Mock<ITeamRepository>();
        teamRepoMock.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        var invitationRepoMock = new Mock<IInvitationRepository>();
        invitationRepoMock.Setup(r => r.GetPendingByTeamIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Invitation>());

        var handler = new GetTeamInvitationsQueryHandler(invitationRepoMock.Object, teamRepoMock.Object);
        var query = new GetTeamInvitationsQuery(team.Id, "owner-1");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        invitationRepoMock.Verify(r => r.GetPendingByTeamIdAsync(team.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}

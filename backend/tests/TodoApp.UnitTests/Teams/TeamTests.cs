using System.Linq;
using TodoApp.Domain.Teams;
using Xunit;

namespace TodoApp.UnitTests.Teams;

/// <summary>
/// Pure domain coverage for Team's membership/ownership rules — previously
/// untested at the domain level at all (only hit indirectly through
/// handler tests), per the 2026-08-11 review's "pure domain tests: none"
/// finding.
/// </summary>
public class TeamTests
{
    [Fact]
    public void Create_SetsCreatorAsOwner()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");

        Assert.True(team.IsMember("owner-1"));
        Assert.Single(team.Members);
        Assert.Equal(TeamRole.Owner, team.Members.Single().Role);
    }

    [Fact]
    public void AddMember_ByNonOwner_ThrowsInvalidOperationException()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        team.AddMember("member-1", "owner-1");

        // member-1 is an ordinary member, not the owner — can't add others.
        Assert.Throws<InvalidOperationException>(() => team.AddMember("member-2", "member-1"));
    }

    [Fact]
    public void AddMember_AlreadyAMember_ThrowsInvalidOperationException()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        team.AddMember("member-1", "owner-1");

        Assert.Throws<InvalidOperationException>(() => team.AddMember("member-1", "owner-1"));
    }

    [Fact]
    public void RemoveMember_TheOwner_ThrowsInvalidOperationException()
    {
        // An owner can't just be removed like an ordinary member — they'd
        // have to transfer ownership first, since a team must always have
        // exactly one owner.
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");

        Assert.Throws<InvalidOperationException>(() => team.RemoveMember("owner-1", "owner-1"));
    }

    [Fact]
    public void RemoveMember_ByNonOwner_ThrowsInvalidOperationException()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        team.AddMember("member-1", "owner-1");
        team.AddMember("member-2", "owner-1");

        Assert.Throws<InvalidOperationException>(() => team.RemoveMember("member-2", "member-1"));
    }

    [Fact]
    public void RemoveMember_OrdinaryMemberByOwner_Succeeds()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        team.AddMember("member-1", "owner-1");

        team.RemoveMember("member-1", "owner-1");

        Assert.False(team.IsMember("member-1"));
    }

    [Fact]
    public void SetMemberRole_ToOwner_ThrowsArgumentException()
    {
        // Ownership has its own transfer flow — SetMemberRole is only for
        // Member <-> Admin.
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        team.AddMember("member-1", "owner-1");

        Assert.Throws<ArgumentException>(() => team.SetMemberRole("member-1", TeamRole.Owner, "owner-1"));
    }

    [Fact]
    public void SetMemberRole_PromoteToAdmin_Succeeds()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        team.AddMember("member-1", "owner-1");

        team.SetMemberRole("member-1", TeamRole.Admin, "owner-1");

        Assert.Equal(TeamRole.Admin, team.Members.Single(m => m.UserId == "member-1").Role);
    }

    [Fact]
    public void SetMemberRole_ByNonOwner_ThrowsInvalidOperationException()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        team.AddMember("member-1", "owner-1");
        team.AddMember("member-2", "owner-1");

        Assert.Throws<InvalidOperationException>(() => team.SetMemberRole("member-2", TeamRole.Admin, "member-1"));
    }

    [Fact]
    public void EnsureIsMember_NonMember_ThrowsInvalidOperationException()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");

        Assert.Throws<InvalidOperationException>(() => team.EnsureIsMember("some-stranger"));
    }

    [Fact]
    public void EnsureIsOwnerOrAdmin_OrdinaryMember_ThrowsInvalidOperationException()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        team.AddMember("member-1", "owner-1");

        Assert.Throws<InvalidOperationException>(() => team.EnsureIsOwnerOrAdmin("member-1"));
    }

    [Fact]
    public void EnsureIsOwnerOrAdmin_Admin_DoesNotThrow()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        team.AddMember("member-1", "owner-1");
        team.SetMemberRole("member-1", TeamRole.Admin, "owner-1");

        var exception = Record.Exception(() => team.EnsureIsOwnerOrAdmin("member-1"));

        Assert.Null(exception);
    }
}

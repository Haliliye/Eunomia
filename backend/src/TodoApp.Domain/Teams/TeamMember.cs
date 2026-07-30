namespace TodoApp.Domain.Teams;

public enum TeamRole
{
    Owner,
    Admin,
    Member
}

/// <summary>
/// Value-object-like member entry inside the Team aggregate.
/// Membership only makes sense in the context of a team, so it is
/// modeled as part of the Team aggregate rather than its own aggregate root.
/// </summary>
public class TeamMember
{
    public string UserId { get; private set; } = string.Empty;
    public TeamRole Role { get; private set; }
    public DateTime JoinedOn { get; private set; }

    private TeamMember() { }

    public TeamMember(string userId, TeamRole role, DateTime joinedOn)
    {
        UserId = userId;
        Role = role;
        JoinedOn = joinedOn;
    }

    public void PromoteToOwner() => Role = TeamRole.Owner;
    public void PromoteToAdmin() => Role = TeamRole.Admin;
    public void DemoteToMember() => Role = TeamRole.Member;
}

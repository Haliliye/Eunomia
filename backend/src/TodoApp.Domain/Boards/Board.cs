using TodoApp.Domain.Common;

namespace TodoApp.Domain.Boards;

/// <summary>
/// A named, revisitable Kanban view within a team — the Board page's "which
/// board am I looking at" concept. The underlying columns are always the
/// team's board columns (see TodoApp.Domain.Teams.BoardColumn — customizable
/// per team, but shared across all of a team's Boards); a Board doesn't define
/// its own columns, it's a saved scope (optionally to one sprint) so someone
/// can jump straight back to e.g. "Sprint 3 Board" instead of re-applying
/// the sprint filter every time. Every team implicitly also has an unsaved
/// "All" view (SprintId null, no Board row needed) shown when nothing here
/// is selected — this class only models the *named, saved* ones.
/// </summary>
public class Board : AggregateRoot
{
    public string TeamId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    /// <summary>Null means this board shows every non-archived story regardless of sprint (same as the default "All" view, just saved under a name).</summary>
    public string? SprintId { get; private set; }

    public DateTime CreatedOn { get; private set; }

    private Board() { }

    private Board(string id, string teamId, string name, string? sprintId) : base(id)
    {
        TeamId = teamId;
        Name = name;
        SprintId = sprintId;
        CreatedOn = DateTime.UtcNow;
    }

    public static Board Create(string id, string teamId, string name, string? sprintId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Board name is required.", nameof(name));

        return new Board(id, teamId, name.Trim(), sprintId);
    }

    public static Board Rehydrate(string id, string teamId, string name, string? sprintId, DateTime createdOn)
    {
        var board = new Board(id, teamId, name, sprintId) { CreatedOn = createdOn };
        return board;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Board name is required.", nameof(name));

        Name = name.Trim();
    }

    public void SetSprint(string? sprintId) => SprintId = sprintId;
}

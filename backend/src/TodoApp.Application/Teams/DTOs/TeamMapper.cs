using TodoApp.Domain.Teams;

namespace TodoApp.Application.Teams.DTOs;

internal static class TeamMapper
{
    public static TeamDto ToDto(Team team) => new(
        team.Id,
        team.Name,
        team.Description,
        team.Members.Select(m => new TeamMemberDto(m.UserId, m.Role.ToString(), m.JoinedOn)).ToList(),
        team.Labels.Select(l => new LabelDto(l.Id, l.Name, l.Color)).ToList(),
        team.WipLimits.Select(w => new ColumnWipLimitDto(w.Status, w.Limit)).ToList(),
        team.Templates.Select(t => new StoryTemplateDto(t.Id, t.Name, t.DefaultDescription, t.DefaultPriority, t.ChecklistItemTexts)).ToList());
}

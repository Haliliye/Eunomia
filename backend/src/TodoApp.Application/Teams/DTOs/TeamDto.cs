namespace TodoApp.Application.Teams.DTOs;

public record TeamMemberDto(string UserId, string Role, DateTime JoinedOn);
public record LabelDto(string Id, string Name, string Color);
public record ColumnWipLimitDto(string Status, int Limit);
public record StoryTemplateDto(string Id, string Name, string? DefaultDescription, string? DefaultPriority, IReadOnlyList<string> ChecklistItemTexts);

public record TeamDto(
    string Id,
    string Name,
    string? Description,
    IReadOnlyCollection<TeamMemberDto> Members,
    IReadOnlyCollection<LabelDto> Labels,
    IReadOnlyCollection<ColumnWipLimitDto> WipLimits,
    IReadOnlyCollection<StoryTemplateDto> Templates);

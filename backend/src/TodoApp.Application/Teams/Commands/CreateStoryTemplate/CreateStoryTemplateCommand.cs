using MediatR;
using TodoApp.Application.Teams.DTOs;

namespace TodoApp.Application.Teams.Commands.CreateStoryTemplate;

public record CreateStoryTemplateCommand(
    string TeamId, string Name, string? DefaultDescription, string? DefaultPriority,
    IReadOnlyList<string> ChecklistItemTexts, string RequestingUserId) : IRequest<StoryTemplateDto>;

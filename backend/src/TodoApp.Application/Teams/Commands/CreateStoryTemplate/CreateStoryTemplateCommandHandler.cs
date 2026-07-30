using MediatR;
using TodoApp.Application.Teams.DTOs;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Teams.Commands.CreateStoryTemplate;

public class CreateStoryTemplateCommandHandler : IRequestHandler<CreateStoryTemplateCommand, StoryTemplateDto>
{
    private readonly ITeamRepository _teamRepository;

    public CreateStoryTemplateCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<StoryTemplateDto> Handle(CreateStoryTemplateCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        var template = team.CreateTemplate(
            Guid.NewGuid().ToString(), request.Name, request.DefaultDescription, request.DefaultPriority,
            request.ChecklistItemTexts, request.RequestingUserId);

        await _teamRepository.UpdateAsync(team, cancellationToken);

        return new StoryTemplateDto(template.Id, template.Name, template.DefaultDescription, template.DefaultPriority, template.ChecklistItemTexts);
    }
}

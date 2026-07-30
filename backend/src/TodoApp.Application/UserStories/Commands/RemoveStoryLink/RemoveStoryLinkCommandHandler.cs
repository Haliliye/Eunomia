using MediatR;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.RemoveStoryLink;

public class RemoveStoryLinkCommandHandler : IRequestHandler<RemoveStoryLinkCommand>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;

    public RemoveStoryLinkCommandHandler(IUserStoryRepository userStoryRepository, ITeamRepository teamRepository)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
    }

    public async Task Handle(RemoveStoryLinkCommand request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.StoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        story.RemoveLink(request.LinkedStoryId);
        await _userStoryRepository.UpdateAsync(story, cancellationToken);

        var linkedStory = await _userStoryRepository.GetByIdAsync(request.LinkedStoryId, cancellationToken);
        if (linkedStory is not null)
        {
            linkedStory.RemoveLink(request.StoryId);
            await _userStoryRepository.UpdateAsync(linkedStory, cancellationToken);
        }
    }
}

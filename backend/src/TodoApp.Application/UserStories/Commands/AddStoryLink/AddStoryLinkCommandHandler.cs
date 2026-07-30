using MediatR;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.AddStoryLink;

public class AddStoryLinkCommandHandler : IRequestHandler<AddStoryLinkCommand>
{
    private static readonly Dictionary<StoryLinkType, StoryLinkType> InverseOf = new()
    {
        [StoryLinkType.Blocks] = StoryLinkType.BlockedBy,
        [StoryLinkType.BlockedBy] = StoryLinkType.Blocks,
        [StoryLinkType.RelatesTo] = StoryLinkType.RelatesTo,
    };

    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;

    public AddStoryLinkCommandHandler(IUserStoryRepository userStoryRepository, ITeamRepository teamRepository)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
    }

    public async Task Handle(AddStoryLinkCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<StoryLinkType>(request.LinkType, out var linkType))
            throw new ArgumentException($"Unknown link type '{request.LinkType}'.");

        var story = await _userStoryRepository.GetByIdAsync(request.StoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        var linkedStory = await _userStoryRepository.GetByIdAsync(request.LinkedStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("The story you're trying to link to wasn't found.");

        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        // Cross-team linking is intentionally allowed (e.g. a shared platform
        // dependency) — only membership on the story actually being edited is
        // enforced; the OTHER story's own team membership rules still apply
        // separately whenever someone edits IT directly.

        story.AddLink(linkedStory.Id, linkType);
        linkedStory.AddLink(story.Id, InverseOf[linkType]);

        await _userStoryRepository.UpdateAsync(story, cancellationToken);
        await _userStoryRepository.UpdateAsync(linkedStory, cancellationToken);
    }
}

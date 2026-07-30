using MediatR;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.SetEstimate;

public class SetEstimateCommandHandler : IRequestHandler<SetEstimateCommand>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;

    public SetEstimateCommandHandler(IUserStoryRepository userStoryRepository, ITeamRepository teamRepository)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
    }

    public async Task Handle(SetEstimateCommand request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        // UserStory.SetEstimate throws ArgumentException for a negative value —
        // surfaces as 400 via the existing exception-to-HTTP-code middleware.
        story.SetEstimate(request.Hours);
        await _userStoryRepository.UpdateAsync(story, cancellationToken);
    }
}

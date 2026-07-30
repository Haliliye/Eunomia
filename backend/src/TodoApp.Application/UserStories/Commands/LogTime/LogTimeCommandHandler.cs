using MediatR;
using TodoApp.Application.UserStories.DTOs;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.LogTime;

public class LogTimeCommandHandler : IRequestHandler<LogTimeCommand, TimeLogEntryDto>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;

    public LogTimeCommandHandler(IUserStoryRepository userStoryRepository, ITeamRepository teamRepository)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
    }

    public async Task<TimeLogEntryDto> Handle(LogTimeCommand request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        var entry = story.LogTime(Guid.NewGuid().ToString(), request.Hours, request.Note, request.RequestingUserId);
        await _userStoryRepository.UpdateAsync(story, cancellationToken);

        return new TimeLogEntryDto(entry.Id, entry.Hours, entry.Note, entry.LoggedByUserId, entry.LoggedOn);
    }
}

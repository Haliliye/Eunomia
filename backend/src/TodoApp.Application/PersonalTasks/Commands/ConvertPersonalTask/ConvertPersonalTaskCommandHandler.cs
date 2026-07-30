using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.DTOs;
using TodoApp.Domain.PersonalTasks;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.PersonalTasks.Commands.ConvertPersonalTask;

public class ConvertPersonalTaskCommandHandler : IRequestHandler<ConvertPersonalTaskCommand, UserStoryDto>
{
    private readonly IPersonalTaskRepository _personalTaskRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public ConvertPersonalTaskCommandHandler(
        IPersonalTaskRepository personalTaskRepository,
        ITeamRepository teamRepository,
        IUserStoryRepository userStoryRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _personalTaskRepository = personalTaskRepository;
        _teamRepository = teamRepository;
        _userStoryRepository = userStoryRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<UserStoryDto> Handle(ConvertPersonalTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _personalTaskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new KeyNotFoundException("Task not found.");

        if (task.OwnerUserId != request.RequestingUserId)
            throw new UnauthorizedAccessException("This isn't your task.");

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        // US-141 AC: "Only teams I am a member of appear as conversion targets" —
        // enforced here too (not just by what the frontend offers), since the
        // frontend's list of choices isn't a security boundary on its own.
        team.EnsureIsMember(request.RequestingUserId);

        var story = UserStory.Create(Guid.NewGuid().ToString(), team.Id, task.Title, task.Description);
        if (task.DueDate.HasValue) story.SetDueDate(task.DueDate);

        await _userStoryRepository.AddAsync(story, cancellationToken);

        task.MarkConverted(story.Id);
        await _personalTaskRepository.UpdateAsync(task, cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(team.Id, new { type = "storyChanged", storyId = story.Id }, cancellationToken);

        return UserStoryMapper.ToDto(story);
    }
}

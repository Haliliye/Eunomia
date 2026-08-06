using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using TodoApp.Domain.Users;

namespace TodoApp.Application.UserStories.Commands.ImportUserStories;

public class ImportUserStoriesCommandHandler : IRequestHandler<ImportUserStoriesCommand, ImportSummaryDto>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public ImportUserStoriesCommandHandler(
        ITeamRepository teamRepository,
        IUserStoryRepository userStoryRepository,
        IUserRepository userRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _teamRepository = teamRepository;
        _userStoryRepository = userStoryRepository;
        _userRepository = userRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<ImportSummaryDto> Handle(ImportUserStoriesCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        var rows = ImportRowParser.ParseAndValidate(request.CsvContent, request.Mapping);
        var result = await UserStoryRowApplier.ApplyAsync(team, rows, _userStoryRepository, _userRepository, request.RequestingUserId, cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(team.Id, new { type = "storyChanged", storyId = (string?)null }, cancellationToken);

        var skippedCount = rows.Count(r => !r.IsValid);
        return new ImportSummaryDto(result.CreatedCount, skippedCount, rows, result.UpdatedCount);
    }
}

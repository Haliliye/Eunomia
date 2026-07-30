using MediatR;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.UserStories.Commands.ImportUserStories;

public class PreviewImportUserStoriesCommandHandler : IRequestHandler<PreviewImportUserStoriesCommand, IReadOnlyList<ImportRowDto>>
{
    private readonly ITeamRepository _teamRepository;

    public PreviewImportUserStoriesCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<IReadOnlyList<ImportRowDto>> Handle(PreviewImportUserStoriesCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        // Import is a bulk-creation/migration action — same permission level
        // as sprint management (owner/admin), not open to every member.
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        return ImportRowParser.ParseAndValidate(request.CsvContent, request.Mapping);
    }
}

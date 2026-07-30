using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.UserStories.Commands.AnalyzeCsv;

public class AnalyzeCsvCommandHandler : IRequestHandler<AnalyzeCsvCommand, CsvAnalysisDto>
{
    // A generous cap, not a hard assumption this app handles huge imports —
    // protects against a single analyze request pulling a truly enormous
    // file's entire contents into memory and the response payload.
    private const int MaxRows = 2000;

    private readonly ITeamRepository _teamRepository;

    public AnalyzeCsvCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<CsvAnalysisDto> Handle(AnalyzeCsvCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        var rows = CsvParser.Parse(request.CsvContent);
        if (rows.Count == 0)
            return new CsvAnalysisDto(Array.Empty<string>(), Array.Empty<IReadOnlyList<string>>(), 0);

        var headers = rows[0];
        var dataRows = rows.Skip(1).ToList();

        // Returns (nearly) the whole dataset, not just a handful of rows —
        // the frontend needs every distinct value in whichever column ends up
        // mapped to Status/Priority to build the value-mapping step (US: "map
        // Jira's 'In Progress' to our 'Dev'"), and it doesn't know which
        // column that'll be until the person picks it, one step later.
        var included = dataRows.Take(MaxRows).Select(r => (IReadOnlyList<string>)r).ToList();

        return new CsvAnalysisDto(headers, included, dataRows.Count);
    }
}

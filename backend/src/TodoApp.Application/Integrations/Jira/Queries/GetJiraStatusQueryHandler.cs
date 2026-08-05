using MediatR;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.Jira.Queries;

public class GetJiraStatusQueryHandler : IRequestHandler<GetJiraStatusQuery, JiraStatusDto>
{
    private readonly IJiraConnectionRepository _connectionRepository;

    public GetJiraStatusQueryHandler(IJiraConnectionRepository connectionRepository)
    {
        _connectionRepository = connectionRepository;
    }

    public async Task<JiraStatusDto> Handle(GetJiraStatusQuery request, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByUserIdAsync(request.RequestingUserId, cancellationToken);
        return connection is null
            ? new JiraStatusDto(false, null, null)
            : new JiraStatusDto(true, connection.SiteName, connection.ConnectedOn);
    }
}

using MediatR;
using TodoApp.Domain.Integrations;

namespace TodoApp.Application.Integrations.GitLab.Queries;

public class GetGitLabStatusQueryHandler : IRequestHandler<GetGitLabStatusQuery, GitLabStatusDto>
{
    private readonly IGitLabConnectionRepository _connectionRepository;

    public GetGitLabStatusQueryHandler(IGitLabConnectionRepository connectionRepository)
    {
        _connectionRepository = connectionRepository;
    }

    public async Task<GitLabStatusDto> Handle(GetGitLabStatusQuery request, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByUserIdAsync(request.RequestingUserId, cancellationToken);
        return connection is null
            ? new GitLabStatusDto(false, null, null)
            : new GitLabStatusDto(true, connection.GitLabUsername, connection.ConnectedOn);
    }
}

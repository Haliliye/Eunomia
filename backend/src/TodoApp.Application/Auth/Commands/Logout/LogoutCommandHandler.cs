using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Auth;

namespace TodoApp.Application.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = TokenHasher.Hash(request.RefreshToken);
        var token = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        // Logging out with an already-invalid/unknown token is a no-op, not
        // an error — the end state (not logged in) is the same either way.
        if (token is not null)
        {
            token.Revoke();
            await _refreshTokenRepository.UpdateAsync(token, cancellationToken);
        }
    }
}

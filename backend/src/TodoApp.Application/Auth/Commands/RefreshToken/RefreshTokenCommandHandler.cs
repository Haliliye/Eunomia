using MediatR;
using TodoApp.Application.Auth.DTOs;
using TodoApp.Application.Common;
using TodoApp.Domain.Auth;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = TokenHasher.Hash(request.RefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (existingToken is null)
            throw new AuthenticationFailedException("Invalid or expired refresh token. Please log in again.");

        // A token that's already been revoked being presented again is a
        // stronger signal than "just expired" — it means either the person
        // is retrying with a stale copy after rotation (harmless), or
        // someone else has a copy of a token that's already been used
        // (token theft). There's no way to tell those apart from here, so
        // the safe response treats it as theft: revoke every token in the
        // family (every device logged in via that original login chain),
        // forcing a fresh login everywhere rather than letting a possibly
        //-stolen session quietly continue on whichever side asks next.
        if (existingToken.RevokedOn is not null)
        {
            await _refreshTokenRepository.RevokeAllInFamilyAsync(existingToken.FamilyId, cancellationToken);
            throw new AuthenticationFailedException("This refresh token was already used. All sessions for this account have been signed out as a precaution — please log in again.");
        }

        if (!existingToken.IsActive)
            throw new AuthenticationFailedException("Invalid or expired refresh token. Please log in again.");

        var user = await _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken)
            ?? throw new AuthenticationFailedException("Invalid or expired refresh token. Please log in again.");

        // Rotate: the used token is revoked and a new one issued in its
        // place, carrying the same FamilyId forward — see RevokeAllInFamilyAsync
        // above for why the family (not just this one token) matters.
        existingToken.Revoke();
        await _refreshTokenRepository.UpdateAsync(existingToken, cancellationToken);

        var newAccessToken = _jwtTokenGenerator.GenerateToken(user);
        var (newRefreshToken, expiresOn) = _jwtTokenGenerator.GenerateRefreshToken();

        var newRefreshTokenEntity = Domain.Auth.RefreshToken.Create(
            Guid.NewGuid().ToString(), user.Id, TokenHasher.Hash(newRefreshToken), expiresOn, existingToken.FamilyId);
        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);

        return new AuthResultDto(newAccessToken, newRefreshToken, user.Id, user.Email, user.DisplayName, user.IsEmailVerified);
    }
}

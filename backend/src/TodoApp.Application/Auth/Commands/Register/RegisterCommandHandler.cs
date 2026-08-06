using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TodoApp.Application.Auth.DTOs;
using TodoApp.Application.Common;
using TodoApp.Domain.Auth;
using TodoApp.Domain.Invitations;
using TodoApp.Domain.Teams;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResultDto>
{
    private const int VerificationExpiryHours = 24;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IEmailVerificationTokenRepository _verificationTokenRepository;
    private readonly IEmailSignupInvitationRepository _signupInvitationRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IEmailSender _emailSender;
    private readonly IEmailSettingsProvider _emailSettings;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        IEmailVerificationTokenRepository verificationTokenRepository,
        IEmailSignupInvitationRepository signupInvitationRepository,
        ITeamRepository teamRepository,
        IEmailSender emailSender,
        IEmailSettingsProvider emailSettings,
        IHostEnvironment environment,
        ILogger<RegisterCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _verificationTokenRepository = verificationTokenRepository;
        _signupInvitationRepository = signupInvitationRepository;
        _teamRepository = teamRepository;
        _emailSender = emailSender;
        _emailSettings = emailSettings;
        _environment = environment;
        _logger = logger;
    }

    public async Task<AuthResultDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var alreadyExists = await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
        if (alreadyExists)
            throw new InvalidOperationException("An account with this email already exists.");

        var user = User.Create(
            id: Guid.NewGuid().ToString(),
            email: request.Email,
            displayName: request.DisplayName,
            passwordHash: _passwordHasher.Hash(request.Password));

        await _userRepository.AddAsync(user, cancellationToken);

        await FulfillPendingTeamInvitationsAsync(user, cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateToken(user);
        var (refreshToken, expiresOn) = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshTokenEntity = Domain.Auth.RefreshToken.Create(Guid.NewGuid().ToString(), user.Id, TokenHasher.Hash(refreshToken), expiresOn);
        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        // Non-blocking: the account is fully usable while unverified — this
        // token just backs the "verify your email" reminder banner and link.
        var rawVerificationToken = TokenHasher.GenerateRawToken();
        var verificationToken = EmailVerificationToken.Create(
            Guid.NewGuid().ToString(), user.Id, TokenHasher.Hash(rawVerificationToken), DateTime.UtcNow.AddHours(VerificationExpiryHours));
        await _verificationTokenRepository.AddAsync(verificationToken, cancellationToken);

        var verificationLink = $"{_emailSettings.FrontendBaseUrl}/verify-email?token={rawVerificationToken}";

        string? devToken = null;
        if (_emailSettings.IsConfigured)
        {
            // Real SMTP is set up — actually send it. Failures here shouldn't
            // block registration (the account is already created and usable;
            // the person can always hit "resend" from the reminder banner).
            try
            {
                await _emailSender.SendAsync(user.Email, "Verify your email", EmailTemplates.VerifyEmail(verificationLink), cancellationToken);
            }
            catch (Exception ex)
            {
                // Still swallowed from the caller's perspective (registration
                // itself already succeeded), but logged now.
                _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
            }
        }
        else if (_environment.IsDevelopment())
        {
            // No SMTP configured — fall back to surfacing the raw token so the
            // flow is still testable locally without a real mail server.
            devToken = rawVerificationToken;
        }

        return new AuthResultDto(accessToken, refreshToken, user.Id, user.Email, user.DisplayName, user.IsEmailVerified, devToken);
    }

    /// <summary>
    /// The other half of ImportFromJiraCommandHandler's signup invitations:
    /// anyone who registers with an email that was invited (as a Jira
    /// assignee with no Eunomia account yet) is auto-added to every team
    /// that invited them, then those invitations are cleared. Best-effort —
    /// a failure here shouldn't block the registration that's already
    /// succeeded by this point.
    /// </summary>
    private async Task FulfillPendingTeamInvitationsAsync(User user, CancellationToken cancellationToken)
    {
        IReadOnlyList<EmailSignupInvitation> invitations;
        try
        {
            invitations = await _signupInvitationRepository.GetByEmailAsync(user.Email, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to look up pending signup invitations for {Email}", user.Email);
            return;
        }

        foreach (var invitation in invitations)
        {
            try
            {
                var team = await _teamRepository.GetByIdAsync(invitation.TeamId, cancellationToken);
                if (team is not null && !team.Members.Any(m => m.UserId == user.Id))
                {
                    team.AddMemberFromInvitation(user.Id);
                    await _teamRepository.UpdateAsync(team, cancellationToken);
                }

                await _signupInvitationRepository.DeleteAsync(invitation.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fulfil signup invitation {InvitationId} for {Email}", invitation.Id, user.Email);
            }
        }
    }
}

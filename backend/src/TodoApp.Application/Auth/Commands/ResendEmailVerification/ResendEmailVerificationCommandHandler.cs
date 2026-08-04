using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TodoApp.Application.Common;
using TodoApp.Domain.Auth;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Auth.Commands.ResendEmailVerification;

public class ResendEmailVerificationCommandHandler : IRequestHandler<ResendEmailVerificationCommand, string?>
{
    private const int ExpiryHours = 24;

    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IEmailSender _emailSender;
    private readonly IEmailSettingsProvider _emailSettings;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ResendEmailVerificationCommandHandler> _logger;

    public ResendEmailVerificationCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository tokenRepository,
        IEmailSender emailSender,
        IEmailSettingsProvider emailSettings,
        IHostEnvironment environment,
        ILogger<ResendEmailVerificationCommandHandler> logger)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _emailSender = emailSender;
        _emailSettings = emailSettings;
        _environment = environment;
        _logger = logger;
    }

    public async Task<string?> Handle(ResendEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.IsEmailVerified)
            return null; // nothing to resend — already verified

        var rawToken = TokenHasher.GenerateRawToken();
        var token = EmailVerificationToken.Create(
            Guid.NewGuid().ToString(), user.Id, TokenHasher.Hash(rawToken), DateTime.UtcNow.AddHours(ExpiryHours));
        await _tokenRepository.AddAsync(token, cancellationToken);

        var verificationLink = $"{_emailSettings.FrontendBaseUrl}/verify-email?token={rawToken}";

        if (_emailSettings.IsConfigured)
        {
            try
            {
                await _emailSender.SendAsync(user.Email, "Verify your email", EmailTemplates.VerifyEmail(verificationLink), cancellationToken);
            }
            catch (Exception ex)
            {
                // Still swallowed from the caller's perspective (a bounced email
                // shouldn't fail the request), but logged now — previously this
                // failed completely silently, which made a real SMTP
                // misconfiguration indistinguishable from "nothing happened".
                _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
            }
            return null;
        }

        return _environment.IsDevelopment() ? rawToken : null;
    }
}

using MediatR;
using Microsoft.Extensions.Hosting;
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

    public ResendEmailVerificationCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository tokenRepository,
        IEmailSender emailSender,
        IEmailSettingsProvider emailSettings,
        IHostEnvironment environment)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _emailSender = emailSender;
        _emailSettings = emailSettings;
        _environment = environment;
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
            catch
            {
                // Swallowed deliberately — same reasoning as RegisterCommandHandler.
            }
            return null;
        }

        return _environment.IsDevelopment() ? rawToken : null;
    }
}

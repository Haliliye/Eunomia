using MediatR;
using Microsoft.Extensions.Hosting;
using TodoApp.Application.Common;
using TodoApp.Domain.Auth;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Auth.Commands.RequestPasswordReset;

public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, string?>
{
    private const int ExpiryMinutes = 60;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _resetTokenRepository;
    private readonly IEmailSender _emailSender;
    private readonly IEmailSettingsProvider _emailSettings;
    private readonly IHostEnvironment _environment;

    public RequestPasswordResetCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository resetTokenRepository,
        IEmailSender emailSender,
        IEmailSettingsProvider emailSettings,
        IHostEnvironment environment)
    {
        _userRepository = userRepository;
        _resetTokenRepository = resetTokenRepository;
        _emailSender = emailSender;
        _emailSettings = emailSettings;
        _environment = environment;
    }

    public async Task<string?> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Deliberately don't throw/reveal whether the email exists — the
        // controller always returns the same "if that email exists..."
        // response regardless, to avoid letting someone enumerate accounts.
        if (user is null) return null;

        var rawToken = TokenHasher.GenerateRawToken();
        var resetToken = PasswordResetToken.Create(
            Guid.NewGuid().ToString(), user.Id, TokenHasher.Hash(rawToken), DateTime.UtcNow.AddMinutes(ExpiryMinutes));

        await _resetTokenRepository.AddAsync(resetToken, cancellationToken);

        var resetLink = $"{_emailSettings.FrontendBaseUrl}/reset-password?token={rawToken}";

        if (_emailSettings.IsConfigured)
        {
            try
            {
                await _emailSender.SendAsync(user.Email, "Reset your password", EmailTemplates.ResetPassword(resetLink), cancellationToken);
            }
            catch
            {
                // Swallowed deliberately — same reasoning as RegisterCommandHandler:
                // don't let a mail-server hiccup surface as an error here, since
                // that would also leak "this email exists" information.
            }
            return null;
        }

        // No SMTP configured — fall back to surfacing the raw token
        // (Development only) so the flow stays testable without a mail server.
        return _environment.IsDevelopment() ? rawToken : null;
    }
}

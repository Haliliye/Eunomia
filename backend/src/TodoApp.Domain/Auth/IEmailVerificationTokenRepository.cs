namespace TodoApp.Domain.Auth;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);
    Task UpdateAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);
}

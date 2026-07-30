namespace TodoApp.Application.Common;

/// <summary>Abstraction so Application doesn't depend on a specific hashing library (implemented in Infrastructure with BCrypt).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

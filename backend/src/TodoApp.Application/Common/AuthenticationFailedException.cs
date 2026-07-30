namespace TodoApp.Application.Common;

/// <summary>
/// Thrown for invalid login credentials — distinct from UnauthorizedAccessException,
/// which the rest of the app uses for "authenticated but not permitted" (403).
/// This maps to 401 in ExceptionHandlingMiddleware.
/// </summary>
public class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message) : base(message) { }
}

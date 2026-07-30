namespace TodoApp.Application.Common;

/// <summary>Thrown when an update's expected version no longer matches what's persisted — someone else saved a change first. Maps to 409 in ExceptionHandlingMiddleware.</summary>
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message) : base(message) { }
}

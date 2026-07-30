using System.Security.Claims;

namespace TodoApp.Api.Common;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The authenticated caller's user id, taken from the JWT — never from a
    /// request body/query field. Controllers use this instead of a client-
    /// supplied "ownerId"/"requestingUserId"/"authorId" so a caller can't
    /// perform actions "as" someone else.
    /// </summary>
    public static string GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("No authenticated user on this request.");
}

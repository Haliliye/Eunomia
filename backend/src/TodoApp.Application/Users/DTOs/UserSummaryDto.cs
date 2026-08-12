namespace TodoApp.Application.Users.DTOs;

/// <summary>Public-safe user info — no password hash, no email unless the caller is the user themselves (kept simple: email is included since only authenticated team members can look each other up).</summary>
// Email deliberately excluded — this endpoint resolves ids to display names
// for UI purposes only (see frontend useUserNames), and any authenticated
// user can call it with arbitrary ids, so it shouldn't leak PII beyond what
// the UI actually needs.
public record UserSummaryDto(string Id, string DisplayName);

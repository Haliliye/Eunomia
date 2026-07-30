namespace TodoApp.Application.Users.DTOs;

/// <summary>Public-safe user info — no password hash, no email unless the caller is the user themselves (kept simple: email is included since only authenticated team members can look each other up).</summary>
public record UserSummaryDto(string Id, string DisplayName, string Email);

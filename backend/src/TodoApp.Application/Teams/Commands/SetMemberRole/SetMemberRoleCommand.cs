using MediatR;

namespace TodoApp.Application.Teams.Commands.SetMemberRole;

/// <summary>NewRole must be "Admin" or "Member" — promoting/demoting to Owner isn't supported here.</summary>
public record SetMemberRoleCommand(string TeamId, string UserId, string NewRole, string RequestingUserId) : IRequest;

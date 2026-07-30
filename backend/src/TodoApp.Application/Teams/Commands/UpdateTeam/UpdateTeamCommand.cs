using MediatR;

namespace TodoApp.Application.Teams.Commands.UpdateTeam;

public record UpdateTeamCommand(string TeamId, string Name, string? Description, string RequestingUserId) : IRequest;

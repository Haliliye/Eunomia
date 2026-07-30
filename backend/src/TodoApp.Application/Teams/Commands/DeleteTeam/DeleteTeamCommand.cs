using MediatR;

namespace TodoApp.Application.Teams.Commands.DeleteTeam;

public record DeleteTeamCommand(string TeamId, string RequestingUserId) : IRequest;

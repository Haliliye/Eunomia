using MediatR;
using TodoApp.Application.Teams.DTOs;

namespace TodoApp.Application.Teams.Commands.CreateTeam;

public record CreateTeamCommand(string Name, string? Description, string OwnerId) : IRequest<TeamDto>;

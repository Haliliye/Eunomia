using MediatR;

namespace TodoApp.Application.Teams.Commands.RemoveTeamMember;

public record RemoveTeamMemberCommand(string TeamId, string UserId, string RequestingUserId) : IRequest;

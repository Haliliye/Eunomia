using MediatR;
using TodoApp.Application.Teams.DTOs;

namespace TodoApp.Application.Teams.Commands.CreateLabel;

public record CreateLabelCommand(string TeamId, string Name, string Color, string RequestingUserId) : IRequest<LabelDto>;

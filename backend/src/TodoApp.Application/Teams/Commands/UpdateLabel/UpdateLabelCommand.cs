using MediatR;

namespace TodoApp.Application.Teams.Commands.UpdateLabel;

public record UpdateLabelCommand(string TeamId, string LabelId, string Name, string Color, string RequestingUserId) : IRequest;

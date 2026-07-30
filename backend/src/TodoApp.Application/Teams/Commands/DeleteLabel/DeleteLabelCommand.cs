using MediatR;

namespace TodoApp.Application.Teams.Commands.DeleteLabel;

public record DeleteLabelCommand(string TeamId, string LabelId, string RequestingUserId) : IRequest;

using MediatR;

namespace TodoApp.Application.Teams.Commands.SetColumnWipLimit;

/// <summary>Limit null removes the cap for that column (US-optional Kanban feature).</summary>
public record SetColumnWipLimitCommand(string TeamId, string Status, int? Limit, string RequestingUserId) : IRequest;

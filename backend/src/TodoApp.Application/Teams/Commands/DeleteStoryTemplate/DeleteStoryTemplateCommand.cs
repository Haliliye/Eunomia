using MediatR;

namespace TodoApp.Application.Teams.Commands.DeleteStoryTemplate;

public record DeleteStoryTemplateCommand(string TeamId, string TemplateId, string RequestingUserId) : IRequest;

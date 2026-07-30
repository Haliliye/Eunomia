using MediatR;

namespace TodoApp.Application.UserStories.Commands.ArchiveUserStory;

public record ArchiveUserStoryCommand(string UserStoryId, string ArchivedByUserId = "") : IRequest;

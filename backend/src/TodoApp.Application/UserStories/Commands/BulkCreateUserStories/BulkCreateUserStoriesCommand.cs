using MediatR;
using TodoApp.Application.UserStories.DTOs;

namespace TodoApp.Application.UserStories.Commands.BulkCreateUserStories;

/// <summary>Each line of a multi-line paste becomes one story — the quick "type a bunch of titles" flow other tools (Trello, Linear) offer.</summary>
public record BulkCreateUserStoriesCommand(string TeamId, IReadOnlyList<string> Titles, string RequestingUserId) : IRequest<IReadOnlyList<UserStoryDto>>;

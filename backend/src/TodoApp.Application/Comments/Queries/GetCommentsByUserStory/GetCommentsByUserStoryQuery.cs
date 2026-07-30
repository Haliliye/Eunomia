using MediatR;
using TodoApp.Application.Comments.DTOs;

namespace TodoApp.Application.Comments.Queries.GetCommentsByUserStory;

public record GetCommentsByUserStoryQuery(string UserStoryId) : IRequest<IReadOnlyList<CommentDto>>;

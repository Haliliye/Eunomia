using FluentValidation;

namespace TodoApp.Application.Comments.Commands.AddComment;

public class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.UserStoryId).NotEmpty();
        RuleFor(x => x.AuthorId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().WithMessage("Comment cannot be empty.");
    }
}

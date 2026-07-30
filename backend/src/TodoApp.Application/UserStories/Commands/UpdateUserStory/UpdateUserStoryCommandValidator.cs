using FluentValidation;

namespace TodoApp.Application.UserStories.Commands.UpdateUserStory;

public class UpdateUserStoryCommandValidator : AbstractValidator<UpdateUserStoryCommand>
{
    public UpdateUserStoryCommandValidator()
    {
        RuleFor(x => x.UserStoryId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");
    }
}

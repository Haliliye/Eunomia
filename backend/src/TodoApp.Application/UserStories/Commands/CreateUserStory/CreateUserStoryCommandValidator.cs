using FluentValidation;

namespace TodoApp.Application.UserStories.Commands.CreateUserStory;

public class CreateUserStoryCommandValidator : AbstractValidator<CreateUserStoryCommand>
{
    public CreateUserStoryCommandValidator()
    {
        RuleFor(x => x.TeamId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");
    }
}

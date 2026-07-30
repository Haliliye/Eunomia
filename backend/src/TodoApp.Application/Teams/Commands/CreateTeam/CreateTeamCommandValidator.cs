using FluentValidation;

namespace TodoApp.Application.Teams.Commands.CreateTeam;

public class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Team name is required.")
            .MaximumLength(50).WithMessage("Team name cannot exceed 50 characters.");

        RuleFor(x => x.OwnerId).NotEmpty();
    }
}

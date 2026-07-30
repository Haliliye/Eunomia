using FluentValidation;

namespace TodoApp.Application.Teams.Commands.InviteTeamMember;

public class InviteTeamMemberCommandValidator : AbstractValidator<InviteTeamMemberCommand>
{
    public InviteTeamMemberCommandValidator()
    {
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Enter a valid email address.");
        RuleFor(x => x.InvitingUserId).NotEmpty();
    }
}

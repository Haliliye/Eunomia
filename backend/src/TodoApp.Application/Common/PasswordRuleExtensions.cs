using FluentValidation;

namespace TodoApp.Application.Common;

/// <summary>Shared password-strength rule so Register and ResetPassword can't drift apart on what counts as a valid password.</summary>
public static class PasswordRuleExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeAStrongPassword<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one symbol.");
}

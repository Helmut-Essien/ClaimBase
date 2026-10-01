using ClaimBase.Domain.Identity;
using FluentValidation;

namespace ClaimBase.Application.Features.Auth;

/// <summary>Rejects login bodies outside the shared field limits before a password hash is touched.</summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>Creates the login rules. Email max 320. Password 8 to 128.</summary>
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .MaximumLength(UserConstraints.EmailMaxLength)
            .EmailAddress();

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(UserConstraints.PasswordMinLength)
            .MaximumLength(UserConstraints.PasswordMaxLength);
    }
}

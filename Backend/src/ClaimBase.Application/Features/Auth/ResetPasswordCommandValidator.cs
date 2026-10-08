using ClaimBase.Domain.Identity;
using FluentValidation;

namespace ClaimBase.Application.Features.Auth;

/// <summary>Rejects a reset body outside the shared password limits, or with two different passwords.</summary>
public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    /// <summary>Creates the rules. Password 8 to 128. Token max 128.</summary>
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .MaximumLength(UserConstraints.EmailMaxLength)
            .EmailAddress();

        RuleFor(command => command.Token)
            .NotEmpty()
            .MaximumLength(UserConstraints.PasswordResetTokenMaxLength);

        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .MinimumLength(UserConstraints.PasswordMinLength)
            .MaximumLength(UserConstraints.PasswordMaxLength);

        RuleFor(command => command.ConfirmPassword)
            .Equal(command => command.NewPassword)
            .WithMessage("Passwords do not match.");
    }
}

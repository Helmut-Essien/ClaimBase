using ClaimBase.Domain.Identity;
using FluentValidation;

namespace ClaimBase.Application.Features.Auth;

/// <summary>Rejects a forgot-password body outside the shared email limit.</summary>
public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    /// <summary>Creates the rules. Email max 320.</summary>
    public ForgotPasswordCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .MaximumLength(UserConstraints.EmailMaxLength)
            .EmailAddress();
    }
}

using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Shared.Auth;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace ClaimBase.Application.Features.Auth;

/// <summary>
/// Accepts one unused link and replaces the password.
/// A bad link, a used link, and an expired link all return the same error.
/// </summary>
public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IPasswordResetStore _store;
    private readonly IResetTokenProtector _tokens;
    private readonly IPasswordHasher _passwords;
    private readonly IClock _clock;

    /// <summary>
    /// Creates the handler.
    /// </summary>
    /// <param name="store">Reset-link store.</param>
    /// <param name="tokens">Token hasher.</param>
    /// <param name="passwords">Password hasher.</param>
    /// <param name="clock">UTC clock.</param>
    public ResetPasswordCommandHandler(
        IPasswordResetStore store,
        IResetTokenProtector tokens,
        IPasswordHasher passwords,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(passwords);
        ArgumentNullException.ThrowIfNull(clock);
        _store = store;
        _tokens = tokens;
        _passwords = passwords;
        _clock = clock;
    }

    /// <summary>
    /// Hashes the new password and spends the link.
    /// </summary>
    /// <param name="request">Email, token, and the new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The success message.</returns>
    /// <exception cref="ValidationException">The link is missing, expired, already used, or belongs to another email.</exception>
    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email.Trim().ToLowerInvariant();
        var changed = await _store.TryResetPasswordAsync(
            email,
            _tokens.Hash(request.Token),
            _passwords.Hash(request.NewPassword),
            _clock.UtcNow,
            cancellationToken);

        if (!changed)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(ResetPasswordRequest.Token), PasswordResetCopy.InvalidToken)
            });
        }

        return new ResetPasswordResponse { Message = PasswordResetCopy.Reset };
    }
}

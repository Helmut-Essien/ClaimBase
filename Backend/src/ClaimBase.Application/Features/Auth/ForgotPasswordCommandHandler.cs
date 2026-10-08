using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Shared.Auth;
using MediatR;

namespace ClaimBase.Application.Features.Auth;

/// <summary>
/// Asks for a reset link when exactly one account uses the email.
/// Unknown and shared addresses get the same response. Link creation and SMTP happen after that response.
/// </summary>
public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private readonly IPasswordResetStore _store;
    private readonly IPasswordResetMailer _mailer;

    /// <summary>
    /// Creates the handler.
    /// </summary>
    /// <param name="store">Account lookup.</param>
    /// <param name="mailer">Queue for the link and the email.</param>
    public ForgotPasswordCommandHandler(IPasswordResetStore store, IPasswordResetMailer mailer)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(mailer);
        _store = store;
        _mailer = mailer;
    }

    /// <summary>
    /// Looks up the email and queues the same follow-up for every result.
    /// </summary>
    /// <param name="request">Email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The public confirmation. It does not include the token.</returns>
    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email.Trim().ToLowerInvariant();
        var account = await _store.FindSingleAccountByEmailAsync(email, cancellationToken);

        // The queue runs for a miss too. Writing the token only on a hit would make that hit slower.
        await _mailer.QueueAsync(email, account, cancellationToken);
        return new ForgotPasswordResponse { Message = PasswordResetCopy.LinkSent };
    }
}

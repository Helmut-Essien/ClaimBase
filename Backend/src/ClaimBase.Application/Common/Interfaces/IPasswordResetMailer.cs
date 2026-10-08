namespace ClaimBase.Application.Common.Interfaces;

/// <summary>
/// Queues reset-link creation and the email. The HTTP request does the same work when the address is unknown.
/// </summary>
public interface IPasswordResetMailer
{
    /// <summary>
    /// Queues one attempt. A null account still queues, and the worker sends nothing.
    /// The raw token is created on the worker, after this method returns, except in tests.
    /// </summary>
    /// <param name="email">Recipient.</param>
    /// <param name="account">The single matching account, or null when the address is missing or shared.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task QueueAsync(string email, PasswordResetAccount? account, CancellationToken cancellationToken);
}

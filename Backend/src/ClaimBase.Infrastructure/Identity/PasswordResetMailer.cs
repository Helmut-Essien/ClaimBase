using ClaimBase.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;

namespace ClaimBase.Infrastructure.Identity;

/// <summary>
/// Enqueues link creation. Tests run it inline so they can read the link before the method returns.
/// </summary>
public sealed class PasswordResetMailer : IPasswordResetMailer
{
    private readonly IHostEnvironment _environment;
    private readonly IPasswordResetEmailQueue _queue;
    private readonly IPasswordResetDelivery _delivery;

    /// <summary>
    /// Creates the mailer.
    /// </summary>
    /// <param name="environment">Host environment.</param>
    /// <param name="queue">Background queue used outside tests.</param>
    /// <param name="delivery">Inline delivery used by the test host.</param>
    public PasswordResetMailer(
        IHostEnvironment environment,
        IPasswordResetEmailQueue queue,
        IPasswordResetDelivery delivery)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(delivery);
        _environment = environment;
        _queue = queue;
        _delivery = delivery;
    }

    /// <inheritdoc />
    public async Task QueueAsync(string email, PasswordResetAccount? account, CancellationToken cancellationToken)
    {
        // Testing finishes inline so the suite can read the link. Every other environment only enqueues,
        // including when the account is null, so an unknown address is not faster than a real one.
        if (_environment.IsEnvironment("Testing"))
        {
            await _delivery.DeliverAsync(email, account, cancellationToken);
            return;
        }

        await _queue.EnqueueAsync(new PasswordResetEmailWork(email, account), cancellationToken);
    }
}

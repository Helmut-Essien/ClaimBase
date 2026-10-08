using ClaimBase.Application.Common;
using ClaimBase.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ClaimBase.Infrastructure.Identity;

/// <summary>Creates the hashed link and sends it. A null account does neither.</summary>
public interface IPasswordResetDelivery
{
    /// <summary>
    /// Issues one link and sends it. Unknown and shared addresses stop here.
    /// </summary>
    /// <param name="email">Recipient.</param>
    /// <param name="account">The single matching account, or null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeliverAsync(string email, PasswordResetAccount? account, CancellationToken cancellationToken);
}

/// <summary>
/// Creates the hashed link and sends it. A null account returns without writing a row or an email.
/// </summary>
public sealed class PasswordResetDelivery : IPasswordResetDelivery
{
    private readonly IPasswordResetStore _store;
    private readonly IResetTokenProtector _tokens;
    private readonly IPortalLinks _links;
    private readonly IClock _clock;
    private readonly EmailOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly IPasswordResetEmailLog _log;
    private readonly SmtpPasswordResetSender _smtp;

    /// <summary>
    /// Creates the delivery step.
    /// </summary>
    /// <param name="store">Reset-link store.</param>
    /// <param name="tokens">Token generator.</param>
    /// <param name="links">Portal link builder.</param>
    /// <param name="clock">UTC clock.</param>
    /// <param name="options">SMTP settings.</param>
    /// <param name="environment">Host environment.</param>
    /// <param name="log">Capture used when SMTP is not configured.</param>
    /// <param name="smtp">SMTP sender.</param>
    public PasswordResetDelivery(
        IPasswordResetStore store,
        IResetTokenProtector tokens,
        IPortalLinks links,
        IClock clock,
        IOptions<EmailOptions> options,
        IHostEnvironment environment,
        IPasswordResetEmailLog log,
        SmtpPasswordResetSender smtp)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(links);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(smtp);
        _store = store;
        _tokens = tokens;
        _links = links;
        _clock = clock;
        _options = options.Value;
        _environment = environment;
        _log = log;
        _smtp = smtp;
    }

    /// <summary>
    /// Issues one link and sends it. Unknown and shared addresses stop here.
    /// </summary>
    /// <param name="email">Recipient.</param>
    /// <param name="account">The single matching account, or null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <inheritdoc />
    public async Task DeliverAsync(string email, PasswordResetAccount? account, CancellationToken cancellationToken)
    {
        if (account is null)
            return;

        var createdAt = _clock.UtcNow;
        var token = _tokens.Create();
        await _store.IssueTokenAsync(
            account,
            token.TokenHash,
            createdAt,
            createdAt.Add(PasswordResetLifetime.Duration),
            cancellationToken);

        // The raw token exists only in this link.
        var resetUrl = _links.ResetPassword(email, token.RawToken);
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _log.Record(email, resetUrl);
            if (_environment.IsDevelopment())
                DevelopmentResetMailbox.Append(_environment.ContentRootPath, email, resetUrl);

            return;
        }

        await _smtp.SendAsync(email, resetUrl, cancellationToken);
    }
}

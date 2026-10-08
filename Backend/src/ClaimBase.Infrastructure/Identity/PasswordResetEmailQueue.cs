using System.Threading.Channels;
using ClaimBase.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ClaimBase.Infrastructure.Identity;

/// <summary>One queued reset attempt. A null account means the worker sends nothing.</summary>
/// <param name="Email">Recipient.</param>
/// <param name="Account">The single matching account, or null.</param>
public sealed record PasswordResetEmailWork(string Email, PasswordResetAccount? Account);

/// <summary>Queue of reset emails. The HTTP request only enqueues.</summary>
public interface IPasswordResetEmailQueue
{
    /// <summary>
    /// Adds one email.
    /// </summary>
    /// <param name="work">Recipient and link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask EnqueueAsync(PasswordResetEmailWork work, CancellationToken cancellationToken);
}

/// <summary>
/// Sends queued reset mail on a background reader so SMTP latency stays off the forgot-password response.
/// This is not a Hangfire job. Hangfire stays reserved for biometric import.
/// </summary>
public sealed class PasswordResetEmailQueue : BackgroundService, IPasswordResetEmailQueue
{
    private readonly Channel<PasswordResetEmailWork> _channel = Channel.CreateUnbounded<PasswordResetEmailWork>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<PasswordResetEmailQueue> _logger;

    /// <summary>
    /// Creates the queue.
    /// </summary>
    /// <param name="scopes">Scope factory for the SMTP sender.</param>
    /// <param name="logger">Logger. Failures are logged without the reset URL.</param>
    public PasswordResetEmailQueue(IServiceScopeFactory scopes, ILogger<PasswordResetEmailQueue> logger)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentNullException.ThrowIfNull(logger);
        _scopes = scopes;
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask EnqueueAsync(PasswordResetEmailWork work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        return _channel.Writer.WriteAsync(work, cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var work in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var delivery = scope.ServiceProvider.GetRequiredService<IPasswordResetDelivery>();
                await delivery.DeliverAsync(work.Email, work.Account, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Password reset email was not sent to {Email}.", work.Email);
            }
        }
    }
}

/// <summary>Sends one reset message over SMTP.</summary>
public sealed class SmtpPasswordResetSender
{
    private readonly EmailOptions _options;

    /// <summary>
    /// Creates the sender.
    /// </summary>
    /// <param name="options">SMTP settings.</param>
    public SmtpPasswordResetSender(IOptions<EmailOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <summary>
    /// Sends the message. Throws when SMTP fails so the queue can log the recipient.
    /// </summary>
    /// <param name="email">Recipient.</param>
    /// <param name="resetUrl">Absolute reset link. Do not log it.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SendAsync(string email, string resetUrl, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(resetUrl);
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
            throw new InvalidOperationException("Email:Host and Email:FromAddress are required to send mail.");

        var (plain, html) = PasswordResetEmail.Compose(resetUrl);
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = PasswordResetEmail.Subject;
        message.Body = new BodyBuilder { TextBody = plain, HtmlBody = html }.ToMessageBody();

        using var client = new SmtpClient();
        // One stalled server must not hold every later reset email behind the default two-minute timeout.
        client.Timeout = 15_000;
        var socket = _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(_options.Host, _options.Port, socket, cancellationToken);
        if (!string.IsNullOrWhiteSpace(_options.Username))
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

namespace ClaimBase.Infrastructure.Identity;

/// <summary>One reset email captured when SMTP is not configured. Tests read this. Production SMTP does not.</summary>
/// <param name="Email">Recipient.</param>
/// <param name="ResetUrl">Absolute link. Treat it as a secret.</param>
public sealed record PasswordResetEmailCapture(string Email, string ResetUrl);

/// <summary>In-memory mailbox used when <c>Email:Host</c> is empty.</summary>
public interface IPasswordResetEmailLog
{
    /// <summary>Copies the messages recorded in this process.</summary>
    /// <returns>The captured emails.</returns>
    IReadOnlyList<PasswordResetEmailCapture> Snapshot();

    /// <summary>
    /// Records one message.
    /// </summary>
    /// <param name="email">Recipient.</param>
    /// <param name="resetUrl">Absolute link.</param>
    void Record(string email, string resetUrl);
}

/// <summary>Thread-safe list behind <see cref="IPasswordResetEmailLog"/>.</summary>
public sealed class PasswordResetEmailLog : IPasswordResetEmailLog
{
    private readonly Lock _gate = new();
    private readonly List<PasswordResetEmailCapture> _messages = [];

    /// <inheritdoc />
    public IReadOnlyList<PasswordResetEmailCapture> Snapshot()
    {
        lock (_gate)
            return _messages.ToArray();
    }

    /// <inheritdoc />
    public void Record(string email, string resetUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(resetUrl);
        lock (_gate)
            _messages.Add(new PasswordResetEmailCapture(email, resetUrl));
    }
}

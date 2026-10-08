namespace ClaimBase.Application.Common;

/// <summary>
/// How long a reset link stays valid. The email copy and the stored expiry both use this value.
/// </summary>
public static class PasswordResetLifetime
{
    /// <summary>Link lifetime in hours. AssetTag uses the same one-hour window.</summary>
    public const int ExpiryHours = 1;

    /// <summary>Link lifetime.</summary>
    public static readonly TimeSpan Duration = TimeSpan.FromHours(ExpiryHours);

    /// <summary>Phrase used in the email body. It must stay in step with <see cref="ExpiryHours"/>.</summary>
    public static readonly string ExpiryLabel = ExpiryHours == 1 ? "1 hour" : $"{ExpiryHours} hours";
}

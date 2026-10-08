namespace ClaimBase.Application.Common.Interfaces;

/// <summary>A raw reset token and the hash that is safe to store.</summary>
/// <param name="RawToken">Token placed in the email link. Shown once.</param>
/// <param name="TokenHash">Lowercase SHA-256 hex of <paramref name="RawToken"/>.</param>
public sealed record ResetToken(string RawToken, string TokenHash);

/// <summary>Creates reset tokens and hashes the value that comes back on the link.</summary>
public interface IResetTokenProtector
{
    /// <summary>Creates a new random token and its hash.</summary>
    /// <returns>The raw token and the hash to store.</returns>
    ResetToken Create();

    /// <summary>
    /// Hashes a token from a reset request.
    /// </summary>
    /// <param name="rawToken">The token from the link.</param>
    /// <returns>Lowercase SHA-256 hex.</returns>
    string Hash(string rawToken);
}

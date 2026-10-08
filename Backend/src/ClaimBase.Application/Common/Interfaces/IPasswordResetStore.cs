namespace ClaimBase.Application.Common.Interfaces;

/// <summary>The one account a reset email may be sent to. Email is unique per tenant, so this exists only when exactly one row matches.</summary>
/// <param name="UserId">User id.</param>
/// <param name="TenantId">Tenant id.</param>
/// <param name="Email">Lowercase email.</param>
public sealed record PasswordResetAccount(string UserId, string TenantId, string Email);

/// <summary>
/// Stores reset links and applies a new password. Anonymous callers have no tenant, so the implementation ignores the tenant filter.
/// </summary>
public interface IPasswordResetStore
{
    /// <summary>
    /// Finds the single user with this lowercase email. Zero rows and two tenants both return null so the caller does not guess.
    /// </summary>
    /// <param name="email">Lowercase email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account, or null when the address is missing or shared.</returns>
    Task<PasswordResetAccount?> FindSingleAccountByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Consumes unused links for this user and stores one new hash.
    /// </summary>
    /// <param name="account">The account the link belongs to.</param>
    /// <param name="tokenHash">Lowercase SHA-256 hex. The raw token is not passed in.</param>
    /// <param name="createdAt">UTC issue time.</param>
    /// <param name="expiresAt">UTC expiry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IssueTokenAsync(
        PasswordResetAccount account,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets the new password when the hash matches an unused, unexpired link for that email.
    /// A wrong email does not spend the link.
    /// </summary>
    /// <param name="email">Lowercase email from the reset form.</param>
    /// <param name="tokenHash">Hash of the token from the link.</param>
    /// <param name="newPasswordHash">Bcrypt hash of the new password.</param>
    /// <param name="usedAt">UTC time of the attempt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the password was changed.</returns>
    Task<bool> TryResetPasswordAsync(
        string email,
        string tokenHash,
        string newPasswordHash,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken);
}

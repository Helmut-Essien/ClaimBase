using ClaimBase.Domain.Common;

namespace ClaimBase.Domain.Identity;

/// <summary>
/// One password-reset link for one user. The database stores the hash. The email carries the raw token.
/// A newer request consumes the previous unused link, and a successful reset consumes the one that was used.
/// </summary>
public sealed class PasswordResetToken
{
    private PasswordResetToken()
    {
    }

    /// <summary>ULID primary key.</summary>
    public string Id { get; private set; } = null!;

    /// <summary>Tenant that owns the user. The global filter uses this. Anonymous reset ignores the filter.</summary>
    public string TenantId { get; private set; } = null!;

    /// <summary>User this link can reset.</summary>
    public string UserId { get; private set; } = null!;

    /// <summary>Lowercase hex SHA-256 of the raw token. Unique. Never the token itself.</summary>
    public string TokenHash { get; private set; } = null!;

    /// <summary>UTC expiry. A link lasts one hour.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>UTC time the link was issued.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>UTC time the link was spent or replaced. Null while it can still be used.</summary>
    public DateTimeOffset? UsedAt { get; private set; }

    /// <summary>
    /// Issues a link. The hash is 64 hex characters. Expiry is after the issue time.
    /// </summary>
    /// <param name="id">ULID.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="userId">User id.</param>
    /// <param name="tokenHash">Lowercase SHA-256 hex of the raw token.</param>
    /// <param name="expiresAt">UTC expiry.</param>
    /// <param name="createdAt">UTC issue time.</param>
    /// <returns>The new token row.</returns>
    public static PasswordResetToken Issue(
        string id,
        string tenantId,
        string userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt)
    {
        var hash = Guard.Required(tokenHash, nameof(tokenHash), UserConstraints.PasswordResetTokenHashLength);
        if (hash.Length != UserConstraints.PasswordResetTokenHashLength || hash.Any(character => !Uri.IsHexDigit(character)))
            throw new ArgumentException("Reset token hash must be 64 hex characters.", nameof(tokenHash));

        var created = ToUtc(createdAt);
        var expires = ToUtc(expiresAt);
        if (expires <= created)
            throw new ArgumentException("Reset token expiry must be after it is issued.", nameof(expiresAt));

        return new PasswordResetToken
        {
            Id = Guard.RequiredId(id, nameof(id), UserConstraints.IdMaxLength),
            TenantId = Guard.RequiredId(tenantId, nameof(tenantId), UserConstraints.IdMaxLength),
            UserId = Guard.RequiredId(userId, nameof(userId), UserConstraints.IdMaxLength),
            TokenHash = hash.ToLowerInvariant(),
            ExpiresAt = expires,
            CreatedAt = created
        };
    }

    /// <summary>
    /// Marks the link spent so it cannot reset the password again.
    /// </summary>
    /// <param name="usedAt">UTC time.</param>
    public void Consume(DateTimeOffset usedAt)
    {
        if (UsedAt is not null)
            throw new InvalidOperationException("This reset link was already used.");

        var utc = ToUtc(usedAt);
        if (utc < CreatedAt)
            throw new ArgumentException("Used time is before the link was issued.", nameof(usedAt));

        UsedAt = utc;
    }

    private static DateTimeOffset ToUtc(DateTimeOffset value) =>
        value.Offset == TimeSpan.Zero ? value : value.ToUniversalTime();
}

using ClaimBase.Domain.Common;

namespace ClaimBase.Domain.Identity;

/// <summary>
/// A university using ClaimBase. Users, courses, and claims belong to one tenant.
/// </summary>
public sealed class Tenant
{
    private Tenant()
    {
    }

    /// <summary>ULID primary key.</summary>
    public string Id { get; private set; } = null!;

    /// <summary>University name shown after sign-in.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>ISO currency copied onto claims. Teaching amounts use this code.</summary>
    public string CurrencyCode { get; private set; } = null!;

    /// <summary>IANA zone used to decide which calendar day a session falls on.</summary>
    public string TimeZoneId { get; private set; } = null!;

    /// <summary>UTC creation time.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Creates a tenant. Currency is stored uppercase. The time zone is the tenant's local calendar.
    /// </summary>
    /// <param name="id">ULID.</param>
    /// <param name="name">University name.</param>
    /// <param name="currencyCode">Three-letter ISO code.</param>
    /// <param name="timeZoneId">IANA time zone id.</param>
    /// <param name="createdAt">UTC timestamp.</param>
    /// <returns>The new tenant.</returns>
    public static Tenant Create(string id, string name, string currencyCode, string timeZoneId, DateTimeOffset createdAt)
    {
        var code = Guard.Required(currencyCode, nameof(currencyCode), TenantConstraints.CurrencyCodeLength).ToUpperInvariant();
        if (code.Length != TenantConstraints.CurrencyCodeLength || code.Any(character => !char.IsAsciiLetter(character)))
            throw new ArgumentException("CurrencyCode must be a three-letter ISO code.", nameof(currencyCode));

        return new Tenant
        {
            Id = Guard.RequiredId(id, nameof(id), UserConstraints.IdMaxLength),
            Name = Guard.Required(name, nameof(name), TenantConstraints.NameMaxLength),
            CurrencyCode = code,
            TimeZoneId = Guard.Required(timeZoneId, nameof(timeZoneId), TenantConstraints.TimeZoneIdMaxLength),
            CreatedAt = ToUtc(createdAt)
        };
    }

    private static DateTimeOffset ToUtc(DateTimeOffset value) =>
        value.Offset == TimeSpan.Zero ? value : value.ToUniversalTime();
}

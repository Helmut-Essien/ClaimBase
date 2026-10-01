namespace ClaimBase.Domain.Identity;

/// <summary>Column bounds for a tenant.</summary>
public static class TenantConstraints
{
    /// <summary>Tenant display name maximum.</summary>
    public const int NameMaxLength = 200;

    /// <summary>ISO currency code length. ClaimBase defaults to GHS.</summary>
    public const int CurrencyCodeLength = 3;

    /// <summary>IANA time zone id maximum. ClaimBase defaults to Africa/Accra.</summary>
    public const int TimeZoneIdMaxLength = 64;
}

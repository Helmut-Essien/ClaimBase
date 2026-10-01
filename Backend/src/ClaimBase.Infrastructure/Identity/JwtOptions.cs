namespace ClaimBase.Infrastructure.Identity;

/// <summary>JWT signing settings from the <c>Jwt</c> configuration section.</summary>
public sealed class JwtOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>HMAC key. Production must be at least 64 characters and must not be the Development key.</summary>
    public string Key { get; set; } = "";

    /// <summary>Token issuer. ClaimBase uses <c>ClaimBase.Api</c>.</summary>
    public string Issuer { get; set; } = "";

    /// <summary>Token audience. ClaimBase uses <c>ClaimBase.Portal</c>.</summary>
    public string Audience { get; set; } = "";
}

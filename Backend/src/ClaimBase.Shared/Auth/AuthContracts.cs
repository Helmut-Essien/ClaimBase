using System.ComponentModel.DataAnnotations;

namespace ClaimBase.Shared.Auth;

/// <summary>
/// Login and profile bounds. These match the domain constraints and the portal <c>AUTH_FIELD_LIMITS</c> constant.
/// </summary>
public static class AuthFieldLimits
{
    /// <summary>Email maximum. Stored lowercase.</summary>
    public const int Email = 320;

    /// <summary>Password minimum.</summary>
    public const int PasswordMin = 8;

    /// <summary>Password maximum.</summary>
    public const int PasswordMax = 128;

    /// <summary>Display name and tenant name maximum.</summary>
    public const int DisplayName = 200;

    /// <summary>ISO currency code length.</summary>
    public const int CurrencyCode = 3;

    /// <summary>IANA time zone id maximum.</summary>
    public const int TimeZoneId = 64;
}

/// <summary>Body for <c>POST /api/auth/login</c>.</summary>
public sealed class LoginRequest
{
    /// <summary>Sign-in email. The handler stores and compares it in lowercase.</summary>
    [Required]
    [MaxLength(AuthFieldLimits.Email)]
    [EmailAddress]
    public string Email { get; init; } = "";

    /// <summary>Plain password. Minimum 8, maximum 128.</summary>
    [Required]
    [MinLength(AuthFieldLimits.PasswordMin)]
    [MaxLength(AuthFieldLimits.PasswordMax)]
    public string Password { get; init; } = "";
}

/// <summary>Successful login. The token is the only credential the portal stores.</summary>
public sealed class AuthResponse
{
    /// <summary>Signed JWT.</summary>
    public required string Token { get; init; }

    /// <summary>UTC expiry.</summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Tenant id.</summary>
    public required string TenantId { get; init; }

    /// <summary>Tenant name.</summary>
    public required string TenantName { get; init; }

    /// <summary>User id.</summary>
    public required string UserId { get; init; }

    /// <summary>Lowercase email.</summary>
    public required string Email { get; init; }

    /// <summary>Display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Role name. <c>Lecturer</c> must not be stored by the portal.</summary>
    public required string Role { get; init; }

    /// <summary>ISO currency code.</summary>
    public required string CurrencyCode { get; init; }
}

/// <summary>Body for <c>GET /api/auth/me</c>. Includes the time zone the shell needs for later calendar days.</summary>
public sealed class MeResponse
{
    /// <summary>Tenant id.</summary>
    public required string TenantId { get; init; }

    /// <summary>Tenant name.</summary>
    public required string TenantName { get; init; }

    /// <summary>User id.</summary>
    public required string UserId { get; init; }

    /// <summary>Lowercase email.</summary>
    public required string Email { get; init; }

    /// <summary>Display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Database role name.</summary>
    public required string Role { get; init; }

    /// <summary>ISO currency code.</summary>
    public required string CurrencyCode { get; init; }

    /// <summary>IANA time zone id.</summary>
    public required string TimeZoneId { get; init; }
}

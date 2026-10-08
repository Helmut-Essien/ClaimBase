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

    /// <summary>Reset token maximum on the wire. The stored value is a 64-character hash.</summary>
    public const int ResetToken = 128;
}

/// <summary>Copy returned by forgot-password and reset-password. The forgot message does not reveal whether the email exists.</summary>
public static class PasswordResetCopy
{
    /// <summary>Response for every forgot-password request.</summary>
    public const string LinkSent = "If that email belongs to a staff account, a reset link is on its way.";

    /// <summary>Response after a link is accepted.</summary>
    public const string Reset = "Password has been reset successfully.";

    /// <summary>Response for a missing, expired, used, or mismatched link.</summary>
    public const string InvalidToken = "Invalid reset token.";
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

/// <summary>Body for <c>POST /api/auth/forgot-password</c>. The response is the same when the email is unknown.</summary>
public sealed class ForgotPasswordRequest
{
    /// <summary>Sign-in email. The handler compares it in lowercase.</summary>
    [Required]
    [MaxLength(AuthFieldLimits.Email)]
    [EmailAddress]
    public string Email { get; init; } = "";
}

/// <summary>Forgot-password result. This type has no token property. The token travels only in the email.</summary>
public sealed class ForgotPasswordResponse
{
    /// <summary>Public confirmation.</summary>
    public required string Message { get; init; }
}

/// <summary>Body for <c>POST /api/auth/reset-password</c>.</summary>
public sealed class ResetPasswordRequest
{
    /// <summary>Email the link was sent to. A different address does not spend the link.</summary>
    [Required]
    [MaxLength(AuthFieldLimits.Email)]
    [EmailAddress]
    public string Email { get; init; } = "";

    /// <summary>Raw token from the link.</summary>
    [Required]
    [MaxLength(AuthFieldLimits.ResetToken)]
    public string Token { get; init; } = "";

    /// <summary>New plain password. Minimum 8, maximum 128.</summary>
    [Required]
    [MinLength(AuthFieldLimits.PasswordMin)]
    [MaxLength(AuthFieldLimits.PasswordMax)]
    public string NewPassword { get; init; } = "";

    /// <summary>Must equal <see cref="NewPassword"/>.</summary>
    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; init; } = "";
}

/// <summary>Reset-password result.</summary>
public sealed class ResetPasswordResponse
{
    /// <summary>Success message. The caller signs in again with the new password.</summary>
    public required string Message { get; init; }
}

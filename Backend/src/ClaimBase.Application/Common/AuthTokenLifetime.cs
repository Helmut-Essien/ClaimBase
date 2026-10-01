namespace ClaimBase.Application.Common;

/// <summary>Access-token lifetime. Login and the issuer share this value.</summary>
public static class AuthTokenLifetime
{
    /// <summary>How long a sign-in token stays valid.</summary>
    public static readonly TimeSpan Duration = TimeSpan.FromHours(8);
}

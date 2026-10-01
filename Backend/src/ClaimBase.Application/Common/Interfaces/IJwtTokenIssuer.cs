namespace ClaimBase.Application.Common.Interfaces;

/// <summary>Issues the portal and mobile access token.</summary>
public interface IJwtTokenIssuer
{
    /// <summary>
    /// Issues a signed JWT. <c>departmentId</c> is included only for a head of department.
    /// </summary>
    /// <param name="account">The authenticated account.</param>
    /// <param name="issuedAt">UTC issue time.</param>
    /// <returns>The token and its expiry.</returns>
    IssuedToken Issue(LoginCandidate account, DateTimeOffset issuedAt);
}

/// <summary>A signed access token.</summary>
/// <param name="Token">Compact JWT.</param>
/// <param name="ExpiresAt">UTC expiry.</param>
public sealed record IssuedToken(string Token, DateTimeOffset ExpiresAt);

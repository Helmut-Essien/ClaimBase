using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClaimBase.Application.Common;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClaimBase.Infrastructure.Identity;

/// <summary>
/// Signs access tokens. Claims are <c>sub</c>, <c>tenantId</c>, <c>role</c>, and <c>departmentId</c> only for a head of department.
/// </summary>
public sealed class JwtTokenIssuer : IJwtTokenIssuer
{
    private readonly JwtOptions _options;

    /// <summary>
    /// Creates the issuer.
    /// </summary>
    /// <param name="options">Signing key, issuer, and audience.</param>
    public JwtTokenIssuer(IOptions<JwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc />
    public IssuedToken Issue(LoginCandidate account, DateTimeOffset issuedAt)
    {
        ArgumentNullException.ThrowIfNull(account);

        var expiresAt = issuedAt.Add(AuthTokenLifetime.Duration);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.UserId),
            new("tenantId", account.TenantId),
            new("role", account.Role.ToString())
        };

        // The portal must not trust a department id from a request body. Only this claim identifies a head of department's department.
        if (account.Role == UserRole.HeadOfDepartment)
        {
            if (string.IsNullOrWhiteSpace(account.DepartmentId))
                throw new InvalidOperationException("Head of department is missing DepartmentId.");

            claims.Add(new Claim("departmentId", account.DepartmentId));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new IssuedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

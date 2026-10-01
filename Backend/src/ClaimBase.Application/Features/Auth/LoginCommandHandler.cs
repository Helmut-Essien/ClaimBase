using ClaimBase.Application.Common.Exceptions;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Shared.Auth;
using MediatR;

namespace ClaimBase.Application.Features.Auth;

/// <summary>
/// Checks the password and issues a JWT. Unknown emails still run a bcrypt compare so they are not faster than a miss.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IIdentityReader _users;
    private readonly IPasswordHasher _passwords;
    private readonly IJwtTokenIssuer _tokens;
    private readonly IClock _clock;

    /// <summary>
    /// Creates the handler.
    /// </summary>
    /// <param name="users">Identity lookups.</param>
    /// <param name="passwords">Password hasher.</param>
    /// <param name="tokens">JWT issuer.</param>
    /// <param name="clock">UTC clock.</param>
    public LoginCommandHandler(IIdentityReader users, IPasswordHasher passwords, IJwtTokenIssuer tokens, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(passwords);
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(clock);
        _users = users;
        _passwords = passwords;
        _tokens = tokens;
        _clock = clock;
    }

    /// <summary>
    /// Signs the user in, or returns the same unauthorized error for an unknown email, a wrong password, and an email used by more than one tenant.
    /// </summary>
    /// <param name="request">Email and password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token and the caller's tenant profile.</returns>
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email.Trim().ToLowerInvariant();
        var matches = await _users.FindByEmailIgnoringTenantAsync(email, cancellationToken);

        // Email is unique per tenant, not globally. Zero or many rows must not reveal which case it was.
        if (matches.Count != 1)
        {
            _passwords.VerifyUnknownEmail(request.Password);
            throw new UnauthorizedAppException();
        }

        var account = matches[0];
        if (!_passwords.Verify(request.Password, account.PasswordHash))
            throw new UnauthorizedAppException();

        var issued = _tokens.Issue(account, _clock.UtcNow);
        return new AuthResponse
        {
            Token = issued.Token,
            ExpiresAt = issued.ExpiresAt,
            TenantId = account.TenantId,
            TenantName = account.TenantName,
            UserId = account.UserId,
            Email = account.Email,
            DisplayName = account.DisplayName,
            Role = account.Role.ToString(),
            CurrencyCode = account.CurrencyCode
        };
    }
}

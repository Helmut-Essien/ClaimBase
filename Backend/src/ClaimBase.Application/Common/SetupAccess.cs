using ClaimBase.Application.Common.Exceptions;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Identity;

namespace ClaimBase.Application.Common;

/// <summary>Loads the signed-in user and allows only tenant admins and admins into academic setup.</summary>
public sealed class SetupAccess : ISetupAccess
{
    private readonly ICurrentTenant _current;
    private readonly IIdentityReader _users;

    /// <summary>
    /// Creates the guard.
    /// </summary>
    /// <param name="current">Tenant from the JWT.</param>
    /// <param name="users">Identity lookups.</param>
    public SetupAccess(ICurrentTenant current, IIdentityReader users)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(users);
        _current = current;
        _users = users;
    }

    /// <inheritdoc />
    public async Task EnsureAsync(CancellationToken cancellationToken)
    {
        if (!_current.IsResolved)
            throw new UnauthorizedAppException();

        var profile = await _users.FindPortalProfileAsync(_current.UserId, cancellationToken);
        if (profile is null)
            throw new NotFoundAppException("User was not found.");

        // The token role can be stale. Academic setup follows the role stored on the user.
        if (profile.Role is not (UserRole.TenantAdmin or UserRole.Admin))
            throw new ForbiddenAppException("Academic setup is limited to tenant admins and admins.");
    }
}

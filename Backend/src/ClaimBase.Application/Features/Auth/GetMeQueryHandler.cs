using ClaimBase.Application.Common.Exceptions;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Identity;
using ClaimBase.Shared.Auth;
using MediatR;

namespace ClaimBase.Application.Features.Auth;

/// <summary>
/// Returns the current user. The database role is checked again so a lecturer token cannot open the portal.
/// </summary>
public sealed class GetMeQueryHandler : IRequestHandler<GetMeQuery, MeResponse>
{
    private readonly IIdentityReader _users;
    private readonly ICurrentTenant _current;

    /// <summary>
    /// Creates the handler.
    /// </summary>
    /// <param name="users">Identity lookups.</param>
    /// <param name="current">Tenant resolved from the JWT.</param>
    public GetMeQueryHandler(IIdentityReader users, ICurrentTenant current)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(current);
        _users = users;
        _current = current;
    }

    /// <summary>
    /// Returns the profile, 401 when no tenant is resolved, 404 when the user is not in that tenant, and 403 for a lecturer.
    /// </summary>
    /// <param name="request">Empty query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The portal profile, including the tenant time zone.</returns>
    public async Task<MeResponse> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        if (!_current.IsResolved)
            throw new UnauthorizedAppException();

        var profile = await _users.FindPortalProfileAsync(_current.UserId, cancellationToken);
        if (profile is null)
            throw new NotFoundAppException("User was not found.");

        // Lecturers use the mobile app. A portal route must not accept this role even when the token is valid.
        if (profile.Role == UserRole.Lecturer)
            throw new ForbiddenAppException("Use the ClaimBase mobile app.");

        return new MeResponse
        {
            TenantId = profile.TenantId,
            TenantName = profile.TenantName,
            UserId = profile.UserId,
            Email = profile.Email,
            DisplayName = profile.DisplayName,
            Role = profile.Role.ToString(),
            CurrencyCode = profile.CurrencyCode,
            TimeZoneId = profile.TimeZoneId
        };
    }
}

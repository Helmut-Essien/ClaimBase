namespace ClaimBase.Application.Common.Interfaces;

/// <summary>
/// Identity lookups. Login ignores the tenant filter. Every other read uses it, so another tenant's id looks missing.
/// </summary>
public interface IIdentityReader
{
    /// <summary>
    /// Finds every user with this lowercase email, across tenants.
    /// Email is unique per tenant, so two universities can share an address and login must not guess.
    /// </summary>
    /// <param name="email">Lowercase email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every match. Empty when nobody uses that email.</returns>
    Task<IReadOnlyList<LoginCandidate>> FindByEmailIgnoringTenantAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Loads the signed-in user inside the resolved tenant. A user id from another tenant returns null.
    /// </summary>
    /// <param name="userId">User id from the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile, or null when the filter hides the row.</returns>
    Task<PortalProfile?> FindPortalProfileAsync(string userId, CancellationToken cancellationToken);
}

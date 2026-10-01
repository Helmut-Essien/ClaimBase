using ClaimBase.Domain.Identity;

namespace ClaimBase.Application.Common.Interfaces;

/// <summary>
/// Tenant and user resolved from the JWT for the current request.
/// Login does not resolve a tenant: email is unique per tenant, not globally.
/// </summary>
public interface ICurrentTenant
{
    /// <summary>True after authentication middleware has read a complete token.</summary>
    bool IsResolved { get; }

    /// <summary>Tenant id from the token. Throws when <see cref="IsResolved"/> is false.</summary>
    string TenantId { get; }

    /// <summary>User id from the <c>sub</c> claim. Throws when <see cref="IsResolved"/> is false.</summary>
    string UserId { get; }

    /// <summary>Role from the token. The database role is still checked before a portal response.</summary>
    UserRole Role { get; }

    /// <summary>Department id when the role is head of department. Otherwise null.</summary>
    string? DepartmentId { get; }
}

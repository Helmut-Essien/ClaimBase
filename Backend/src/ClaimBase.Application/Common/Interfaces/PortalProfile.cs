using ClaimBase.Domain.Identity;

namespace ClaimBase.Application.Common.Interfaces;

/// <summary>Signed-in profile for <c>GET /api/auth/me</c>. Password hashes are not loaded.</summary>
/// <param name="UserId">User id.</param>
/// <param name="TenantId">Tenant id.</param>
/// <param name="Email">Lowercase email.</param>
/// <param name="DisplayName">Display name.</param>
/// <param name="Role">Database role. This wins over a stale token role.</param>
/// <param name="TenantName">Tenant name.</param>
/// <param name="CurrencyCode">Tenant currency.</param>
/// <param name="TimeZoneId">IANA zone for calendar days.</param>
public sealed record PortalProfile(
    string UserId,
    string TenantId,
    string Email,
    string DisplayName,
    UserRole Role,
    string TenantName,
    string CurrencyCode,
    string TimeZoneId);

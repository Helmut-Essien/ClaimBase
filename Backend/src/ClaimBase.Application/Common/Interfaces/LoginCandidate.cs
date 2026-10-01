using ClaimBase.Domain.Identity;

namespace ClaimBase.Application.Common.Interfaces;

/// <summary>Account data needed to check a password and issue a token. The hash never leaves the server.</summary>
/// <param name="UserId">User id.</param>
/// <param name="TenantId">Tenant id.</param>
/// <param name="Email">Lowercase email.</param>
/// <param name="DisplayName">Display name.</param>
/// <param name="PasswordHash">Bcrypt hash.</param>
/// <param name="Role">Role.</param>
/// <param name="DepartmentId">Department id for a head of department.</param>
/// <param name="TenantName">Tenant name copied into the login response.</param>
/// <param name="CurrencyCode">Tenant currency.</param>
public sealed record LoginCandidate(
    string UserId,
    string TenantId,
    string Email,
    string DisplayName,
    string PasswordHash,
    UserRole Role,
    string? DepartmentId,
    string TenantName,
    string CurrencyCode);

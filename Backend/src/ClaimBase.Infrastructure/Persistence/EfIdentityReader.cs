using ClaimBase.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClaimBase.Infrastructure.Persistence;

/// <summary>EF implementation of identity reads.</summary>
public sealed class EfIdentityReader : IIdentityReader
{
    private readonly AppDbContext _db;

    /// <summary>
    /// Creates the reader.
    /// </summary>
    /// <param name="db">The request database context.</param>
    public EfIdentityReader(AppDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LoginCandidate>> FindByEmailIgnoringTenantAsync(
        string email,
        CancellationToken cancellationToken)
    {
        // Login is anonymous. The tenant filter would hide every row, and email is not unique across universities.
        return await _db.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(user => user.Email == email)
            .Join(
                _db.Tenants.IgnoreQueryFilters().AsNoTracking(),
                user => user.TenantId,
                tenant => tenant.Id,
                (user, tenant) => new LoginCandidate(
                    user.Id,
                    user.TenantId,
                    user.Email,
                    user.DisplayName,
                    user.PasswordHash,
                    user.Role,
                    user.DepartmentId,
                    tenant.Name,
                    tenant.CurrencyCode,
                    user.PasswordChangedAt))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<PortalProfile?> FindPortalProfileAsync(string userId, CancellationToken cancellationToken)
    {
        // Filters stay on. A user id from another tenant is not found.
        return _db.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Join(
                _db.Tenants.AsNoTracking(),
                user => user.TenantId,
                tenant => tenant.Id,
                (user, tenant) => new PortalProfile(
                    user.Id,
                    user.TenantId,
                    user.Email,
                    user.DisplayName,
                    user.Role,
                    tenant.Name,
                    tenant.CurrencyCode,
                    tenant.TimeZoneId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

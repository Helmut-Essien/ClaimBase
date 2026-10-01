using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClaimBase.Infrastructure.Persistence;

/// <summary>
/// EF Core model for ClaimBase. The tenant filter is closed until <see cref="ICurrentTenant"/> is resolved.
/// </summary>
public sealed class AppDbContext : DbContext
{
    private readonly string _tenantId;

    /// <summary>
    /// Creates the context. The tenant id is captured now, so callers must resolve the current tenant before this context.
    /// An unresolved tenant captures an empty id, which matches no row. Login and the development seed call <c>IgnoreQueryFilters</c>.
    /// </summary>
    /// <param name="options">Database options.</param>
    /// <param name="currentTenant">Tenant for this scope.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant currentTenant)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(currentTenant);
        _tenantId = currentTenant.IsResolved ? currentTenant.TenantId : string.Empty;
    }

    /// <summary>Tenants.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Users.</summary>
    public DbSet<User> Users => Set<User>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Empty _tenantId matches nothing. That is deliberate: a query without a resolved tenant must not scan every university.
        modelBuilder.Entity<Tenant>().HasQueryFilter(tenant => tenant.Id == _tenantId);
        modelBuilder.Entity<User>().HasQueryFilter(user => user.TenantId == _tenantId);
    }
}

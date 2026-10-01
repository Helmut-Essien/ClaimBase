using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ClaimBase.Infrastructure.Persistence;

/// <summary>
/// Inserts one development university and one user per role. The password is read from configuration and is not in source.
/// Department and staff rows do not exist yet; the head of department and the lecturer still store those ids.
/// </summary>
public static class DevelopmentSeed
{
    /// <summary>Development tenant id.</summary>
    public const string TenantId = "01JB0000000000000000000001";

    /// <summary>Development tenant admin user id.</summary>
    public const string TenantAdminUserId = "01JB0000000000000000000002";

    /// <summary>Development admin user id.</summary>
    public const string AdminUserId = "01JB0000000000000000000003";

    /// <summary>Development head of department user id.</summary>
    public const string HeadOfDepartmentUserId = "01JB0000000000000000000004";

    /// <summary>Development finance user id.</summary>
    public const string FinanceUserId = "01JB0000000000000000000005";

    /// <summary>Development lecturer user id.</summary>
    public const string LecturerUserId = "01JB0000000000000000000006";

    /// <summary>Placeholder department id until the Department table exists.</summary>
    public const string DepartmentId = "01JB0000000000000000000007";

    /// <summary>Placeholder staff id until the Staff table exists.</summary>
    public const string StaffId = "01JB0000000000000000000008";

    /// <summary>Tenant admin email.</summary>
    public const string TenantAdminEmail = "tenantadmin@claimbase.test";

    /// <summary>Admin email.</summary>
    public const string AdminEmail = "admin@claimbase.test";

    /// <summary>Head of department email.</summary>
    public const string HeadOfDepartmentEmail = "hod@claimbase.test";

    /// <summary>Finance email.</summary>
    public const string FinanceEmail = "finance@claimbase.test";

    /// <summary>Lecturer email.</summary>
    public const string LecturerEmail = "lecturer@claimbase.test";

    /// <summary>
    /// Inserts the development tenant when it is missing. Does nothing on later startups.
    /// </summary>
    /// <param name="db">Database context. The caller has not resolved a tenant, so this method ignores query filters.</param>
    /// <param name="passwords">Password hasher.</param>
    /// <param name="configuration">Host configuration. <c>Seed:Password</c> is required.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ApplyAsync(
        AppDbContext db,
        IPasswordHasher passwords,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(passwords);
        ArgumentNullException.ThrowIfNull(configuration);

        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(tenant => tenant.Id == TenantId, cancellationToken))
            return;

        var password = configuration["Seed:Password"];
        if (string.IsNullOrWhiteSpace(password) || password.Length < UserConstraints.PasswordMinLength)
            throw new InvalidOperationException("Seed:Password is required in Development and must be at least 8 characters.");

        if (password.Length > UserConstraints.PasswordMaxLength)
            throw new InvalidOperationException("Seed:Password must be at most 128 characters.");

        var createdAt = DateTimeOffset.UtcNow;
        var hash = passwords.Hash(password);
        var tenant = Tenant.Create(TenantId, "Development University", "GHS", "Africa/Accra", createdAt);

        db.Tenants.Add(tenant);
        db.Users.Add(User.Create(TenantAdminUserId, TenantId, TenantAdminEmail, "Dev Tenant Admin", hash, UserRole.TenantAdmin, null, null, createdAt));
        db.Users.Add(User.Create(AdminUserId, TenantId, AdminEmail, "Dev Admin", hash, UserRole.Admin, null, null, createdAt));
        db.Users.Add(User.Create(HeadOfDepartmentUserId, TenantId, HeadOfDepartmentEmail, "Dev Head of Department", hash, UserRole.HeadOfDepartment, DepartmentId, null, createdAt));
        db.Users.Add(User.Create(FinanceUserId, TenantId, FinanceEmail, "Dev Finance", hash, UserRole.Finance, null, null, createdAt));
        db.Users.Add(User.Create(LecturerUserId, TenantId, LecturerEmail, "Dev Lecturer", hash, UserRole.Lecturer, null, StaffId, createdAt));
        await db.SaveChangesAsync(cancellationToken);
    }
}

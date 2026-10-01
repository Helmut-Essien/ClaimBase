using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Academic;
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

    /// <summary>Faculties.</summary>
    public DbSet<Faculty> Faculties => Set<Faculty>();

    /// <summary>Departments.</summary>
    public DbSet<Department> Departments => Set<Department>();

    /// <summary>Semesters.</summary>
    public DbSet<Semester> Semesters => Set<Semester>();

    /// <summary>Qualifications.</summary>
    public DbSet<Qualification> Qualifications => Set<Qualification>();

    /// <summary>Position titles.</summary>
    public DbSet<PositionTitle> PositionTitles => Set<PositionTitle>();

    /// <summary>Courses.</summary>
    public DbSet<Course> Courses => Set<Course>();

    /// <summary>Staff.</summary>
    public DbSet<Staff> Staff => Set<Staff>();

    /// <summary>Staff department assignments.</summary>
    public DbSet<StaffDepartment> StaffDepartments => Set<StaffDepartment>();

    /// <summary>Staff position appointments.</summary>
    public DbSet<StaffPosition> StaffPositions => Set<StaffPosition>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Empty _tenantId matches nothing. That is deliberate: a query without a resolved tenant must not scan every university.
        modelBuilder.Entity<Tenant>().HasQueryFilter(tenant => tenant.Id == _tenantId);
        modelBuilder.Entity<User>().HasQueryFilter(user => user.TenantId == _tenantId);
        modelBuilder.Entity<Faculty>().HasQueryFilter(faculty => faculty.TenantId == _tenantId);
        modelBuilder.Entity<Department>().HasQueryFilter(department => department.TenantId == _tenantId);
        modelBuilder.Entity<Semester>().HasQueryFilter(semester => semester.TenantId == _tenantId);
        modelBuilder.Entity<Qualification>().HasQueryFilter(qualification => qualification.TenantId == _tenantId);
        modelBuilder.Entity<PositionTitle>().HasQueryFilter(title => title.TenantId == _tenantId);
        modelBuilder.Entity<Course>().HasQueryFilter(course => course.TenantId == _tenantId);
        modelBuilder.Entity<Staff>().HasQueryFilter(staff => staff.TenantId == _tenantId);
        modelBuilder.Entity<StaffDepartment>().HasQueryFilter(assignment => assignment.TenantId == _tenantId);
        modelBuilder.Entity<StaffPosition>().HasQueryFilter(position => position.TenantId == _tenantId);
    }
}

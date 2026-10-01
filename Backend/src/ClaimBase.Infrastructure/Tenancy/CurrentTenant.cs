using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Identity;

namespace ClaimBase.Infrastructure.Tenancy;

/// <summary>
/// Request-scoped tenant. Authentication middleware calls <see cref="Set"/> before any database context is created.
/// </summary>
public sealed class CurrentTenant : ICurrentTenant
{
    private string _tenantId = "";
    private string _userId = "";
    private UserRole _role;
    private string? _departmentId;

    /// <inheritdoc />
    public bool IsResolved { get; private set; }

    /// <inheritdoc />
    public string TenantId => IsResolved ? _tenantId : throw new InvalidOperationException("Tenant is not resolved.");

    /// <inheritdoc />
    public string UserId => IsResolved ? _userId : throw new InvalidOperationException("Tenant is not resolved.");

    /// <inheritdoc />
    public UserRole Role => IsResolved ? _role : throw new InvalidOperationException("Tenant is not resolved.");

    /// <inheritdoc />
    public string? DepartmentId => IsResolved ? _departmentId : throw new InvalidOperationException("Tenant is not resolved.");

    /// <summary>
    /// Records the token claims for this request. Call this before resolving <c>AppDbContext</c> so the query filter captures the id.
    /// </summary>
    /// <param name="tenantId">Tenant id.</param>
    /// <param name="userId">User id.</param>
    /// <param name="role">Role.</param>
    /// <param name="departmentId">Department id for a head of department.</param>
    public void Set(string tenantId, string userId, UserRole role, string? departmentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        _tenantId = tenantId;
        _userId = userId;
        _role = role;
        _departmentId = departmentId;
        IsResolved = true;
    }
}

using ClaimBase.Domain.Common;
using ClaimBase.Domain.Identity;

namespace ClaimBase.Domain.Academic;

/// <summary>Assigns one lecturer to one department. A lecturer may have several. The last one cannot be removed.</summary>
public sealed class StaffDepartment
{
    private StaffDepartment()
    {
    }

    /// <summary>ULID primary key.</summary>
    public string Id { get; private set; } = null!;

    /// <summary>Owning tenant.</summary>
    public string TenantId { get; private set; } = null!;

    /// <summary>Lecturer.</summary>
    public string StaffId { get; private set; } = null!;

    /// <summary>Department.</summary>
    public string DepartmentId { get; private set; } = null!;

    /// <summary>
    /// Creates an assignment.
    /// </summary>
    /// <param name="id">ULID.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="staffId">Lecturer.</param>
    /// <param name="departmentId">Department.</param>
    /// <returns>The new assignment.</returns>
    public static StaffDepartment Create(string id, string tenantId, string staffId, string departmentId) =>
        new()
        {
            Id = Guard.RequiredId(id, nameof(id), UserConstraints.IdMaxLength),
            TenantId = Guard.RequiredId(tenantId, nameof(tenantId), UserConstraints.IdMaxLength),
            StaffId = Guard.RequiredId(staffId, nameof(staffId), UserConstraints.IdMaxLength),
            DepartmentId = Guard.RequiredId(departmentId, nameof(departmentId), UserConstraints.IdMaxLength)
        };
}

using ClaimBase.Domain.Common;
using ClaimBase.Domain.Identity;

namespace ClaimBase.Domain.Academic;

/// <summary>A department under one faculty. A lecturer is assigned to one or more of these.</summary>
public sealed class Department
{
    private Department()
    {
    }

    /// <summary>ULID primary key.</summary>
    public string Id { get; private set; } = null!;

    /// <summary>Owning tenant.</summary>
    public string TenantId { get; private set; } = null!;

    /// <summary>Faculty this department belongs to. A department cannot exist without one.</summary>
    public string FacultyId { get; private set; } = null!;

    /// <summary>Department name, unique inside the faculty.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Creates a department.
    /// </summary>
    /// <param name="id">ULID.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="facultyId">Parent faculty.</param>
    /// <param name="name">Department name.</param>
    /// <returns>The new department.</returns>
    public static Department Create(string id, string tenantId, string facultyId, string name) =>
        new()
        {
            Id = Guard.RequiredId(id, nameof(id), UserConstraints.IdMaxLength),
            TenantId = Guard.RequiredId(tenantId, nameof(tenantId), UserConstraints.IdMaxLength),
            FacultyId = Guard.RequiredId(facultyId, nameof(facultyId), UserConstraints.IdMaxLength),
            Name = Guard.Required(name, nameof(name), AcademicConstraints.NameMaxLength)
        };
}

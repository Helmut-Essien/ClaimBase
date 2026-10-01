using ClaimBase.Domain.Common;
using ClaimBase.Domain.Identity;

namespace ClaimBase.Domain.Academic;

/// <summary>Tenant lookup for the qualification a course awards. The rate matrix uses this, not a fixed enum.</summary>
public sealed class Qualification
{
    private Qualification()
    {
    }

    /// <summary>ULID primary key.</summary>
    public string Id { get; private set; } = null!;

    /// <summary>Owning tenant.</summary>
    public string TenantId { get; private set; } = null!;

    /// <summary>Qualification name, unique in the tenant.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Creates a qualification.
    /// </summary>
    /// <param name="id">ULID.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="name">Qualification name.</param>
    /// <returns>The new qualification.</returns>
    public static Qualification Create(string id, string tenantId, string name) =>
        new()
        {
            Id = Guard.RequiredId(id, nameof(id), UserConstraints.IdMaxLength),
            TenantId = Guard.RequiredId(tenantId, nameof(tenantId), UserConstraints.IdMaxLength),
            Name = Guard.Required(name, nameof(name), AcademicConstraints.QualificationNameMaxLength)
        };
}

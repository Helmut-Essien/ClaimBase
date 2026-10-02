using ClaimBase.Domain.Common;
using ClaimBase.Domain.Identity;

namespace ClaimBase.Domain.Academic;

/// <summary>
/// A campus of one university. Faculties belong to a campus. Rates and semesters stay on the tenant.
/// </summary>
public sealed class Campus
{
    private Campus()
    {
    }

    /// <summary>ULID primary key.</summary>
    public string Id { get; private set; } = null!;

    /// <summary>Owning tenant.</summary>
    public string TenantId { get; private set; } = null!;

    /// <summary>Campus name, unique in the tenant.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Creates a campus.
    /// </summary>
    /// <param name="id">ULID.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="name">Campus name.</param>
    /// <returns>The new campus.</returns>
    public static Campus Create(string id, string tenantId, string name) =>
        new()
        {
            Id = Guard.RequiredId(id, nameof(id), UserConstraints.IdMaxLength),
            TenantId = Guard.RequiredId(tenantId, nameof(tenantId), UserConstraints.IdMaxLength),
            Name = Guard.Required(name, nameof(name), AcademicConstraints.NameMaxLength)
        };
}

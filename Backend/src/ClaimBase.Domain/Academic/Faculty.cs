using ClaimBase.Domain.Common;
using ClaimBase.Domain.Identity;

namespace ClaimBase.Domain.Academic;

/// <summary>A faculty inside one university. Department names are unique under this faculty, not across the tenant.</summary>
public sealed class Faculty
{
    private Faculty()
    {
    }

    /// <summary>ULID primary key.</summary>
    public string Id { get; private set; } = null!;

    /// <summary>Owning tenant.</summary>
    public string TenantId { get; private set; } = null!;

    /// <summary>Faculty name, unique in the tenant.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Creates a faculty.
    /// </summary>
    /// <param name="id">ULID.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="name">Faculty name.</param>
    /// <returns>The new faculty.</returns>
    public static Faculty Create(string id, string tenantId, string name) =>
        new()
        {
            Id = Guard.RequiredId(id, nameof(id), UserConstraints.IdMaxLength),
            TenantId = Guard.RequiredId(tenantId, nameof(tenantId), UserConstraints.IdMaxLength),
            Name = Guard.Required(name, nameof(name), AcademicConstraints.NameMaxLength)
        };
}

using ClaimBase.Domain.Common;
using ClaimBase.Domain.Identity;

namespace ClaimBase.Domain.Academic;

/// <summary>Shared title catalog used by the rate matrix and by staff appointments. It is not itself an appointment.</summary>
public sealed class PositionTitle
{
    private PositionTitle()
    {
    }

    /// <summary>ULID primary key.</summary>
    public string Id { get; private set; } = null!;

    /// <summary>Owning tenant.</summary>
    public string TenantId { get; private set; } = null!;

    /// <summary>Title name, unique in the tenant.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Creates a position title.
    /// </summary>
    /// <param name="id">ULID.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="name">Title name.</param>
    /// <returns>The new title.</returns>
    public static PositionTitle Create(string id, string tenantId, string name) =>
        new()
        {
            Id = Guard.RequiredId(id, nameof(id), UserConstraints.IdMaxLength),
            TenantId = Guard.RequiredId(tenantId, nameof(tenantId), UserConstraints.IdMaxLength),
            Name = Guard.Required(name, nameof(name), AcademicConstraints.PositionTitleNameMaxLength)
        };
}

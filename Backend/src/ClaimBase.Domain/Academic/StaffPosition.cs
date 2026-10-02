using ClaimBase.Domain.Common;
using ClaimBase.Domain.Identity;

namespace ClaimBase.Domain.Academic;

/// <summary>
/// One appointment of a lecturer to a shared position title. Ranges are half-open and must not overlap for the same lecturer.
/// This is the record the claim engine will read. It is not a standalone position screen.
/// </summary>
public sealed class StaffPosition
{
    private StaffPosition()
    {
    }

    /// <summary>ULID primary key.</summary>
    public string Id { get; private set; } = null!;

    /// <summary>Owning tenant.</summary>
    public string TenantId { get; private set; } = null!;

    /// <summary>Lecturer.</summary>
    public string StaffId { get; private set; } = null!;

    /// <summary>Shared title from the rate catalog.</summary>
    public string PositionTitleId { get; private set; } = null!;

    /// <summary>First day included.</summary>
    public DateOnly EffectiveFrom { get; private set; }

    /// <summary>First day excluded. Null means the appointment has not ended.</summary>
    public DateOnly? EffectiveTo { get; private set; }

    /// <summary>The half-open range this appointment covers.</summary>
    public DateRange Range => new(EffectiveFrom, EffectiveTo);

    /// <summary>
    /// Creates an appointment.
    /// </summary>
    /// <param name="id">ULID.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="staffId">Lecturer.</param>
    /// <param name="positionTitleId">Shared title.</param>
    /// <param name="effectiveFrom">First day included.</param>
    /// <param name="effectiveTo">First day excluded, or null.</param>
    /// <returns>The new appointment.</returns>
    public static StaffPosition Create(
        string id,
        string tenantId,
        string staffId,
        string positionTitleId,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo)
    {
        // A missing JSON date binds as 0001-01-01. That is not an appointment start.
        if (effectiveFrom == default)
            throw new ArgumentException("EffectiveFrom is required.", nameof(effectiveFrom));

        if (effectiveTo is not null && effectiveTo.Value <= effectiveFrom)
            throw new ArgumentException("EffectiveTo must be after EffectiveFrom.", nameof(effectiveTo));

        return new StaffPosition
        {
            Id = Guard.RequiredId(id, nameof(id), UserConstraints.IdMaxLength),
            TenantId = Guard.RequiredId(tenantId, nameof(tenantId), UserConstraints.IdMaxLength),
            StaffId = Guard.RequiredId(staffId, nameof(staffId), UserConstraints.IdMaxLength),
            PositionTitleId = Guard.RequiredId(positionTitleId, nameof(positionTitleId), UserConstraints.IdMaxLength),
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo
        };
    }
}

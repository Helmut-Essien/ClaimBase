using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Common;
using ClaimBase.Domain.Identity;

namespace ClaimBase.Domain.Rates;

/// <summary>
/// One transport amount for the whole university, paid once per teaching day for every position.
/// It is not hourly and does not depend on rank or qualification. A null end date stays in force until management replaces it.
/// </summary>
public sealed class TransportRate
{
    private TransportRate()
    {
    }

    /// <summary>ULID primary key.</summary>
    public string Id { get; private set; } = null!;

    /// <summary>Owning tenant. There is one transport timeline per university.</summary>
    public string TenantId { get; private set; } = null!;

    /// <summary>Cedis paid once per teaching day, in the tenant currency.</summary>
    public decimal Amount { get; private set; }

    /// <summary>First day included.</summary>
    public DateOnly EffectiveFrom { get; private set; }

    /// <summary>First day excluded. Null means the amount has not been replaced.</summary>
    public DateOnly? EffectiveTo { get; private set; }

    /// <summary>The half-open range this amount covers.</summary>
    public DateRange Range => new(EffectiveFrom, EffectiveTo);

    /// <summary>
    /// Ends an open-ended rate on <paramref name="effectiveTo"/>. A row that already has an end is not rewritten.
    /// </summary>
    /// <param name="effectiveTo">First day excluded. This is the start of the replacement rate.</param>
    public void EndOn(DateOnly effectiveTo)
    {
        if (EffectiveTo is not null)
            throw new InvalidOperationException("A transport rate that already has an end date cannot be changed.");

        if (effectiveTo <= EffectiveFrom)
            throw new ArgumentException("EffectiveTo must be after EffectiveFrom.", nameof(effectiveTo));

        EffectiveTo = effectiveTo;
    }

    /// <summary>
    /// Creates a transport rate.
    /// </summary>
    /// <param name="id">ULID.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="amount">Cedis per teaching day.</param>
    /// <param name="effectiveFrom">First day included.</param>
    /// <param name="effectiveTo">First day excluded, or null.</param>
    /// <returns>The new rate.</returns>
    public static TransportRate Create(
        string id,
        string tenantId,
        decimal amount,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo)
    {
        if (!RateConstraints.IsStorable(amount))
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be zero or greater with at most 2 decimal places.");

        // A missing JSON date binds as 0001-01-01. That is not a schedule start.
        if (effectiveFrom == default)
            throw new ArgumentException("EffectiveFrom is required.", nameof(effectiveFrom));

        if (effectiveTo is not null && effectiveTo.Value <= effectiveFrom)
            throw new ArgumentException("EffectiveTo must be after EffectiveFrom.", nameof(effectiveTo));

        return new TransportRate
        {
            Id = Guard.RequiredId(id, nameof(id), UserConstraints.IdMaxLength),
            TenantId = Guard.RequiredId(tenantId, nameof(tenantId), UserConstraints.IdMaxLength),
            Amount = amount,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo
        };
    }
}

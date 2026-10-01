namespace ClaimBase.Domain.Academic;

/// <summary>
/// Half-open appointment range <c>[Start, End)</c>. A null end means the appointment is still open.
/// Touching ranges do not overlap: one may end on the day the next starts.
/// </summary>
/// <param name="Start">First day included.</param>
/// <param name="End">First day excluded. Null means no end.</param>
public readonly record struct DateRange(DateOnly Start, DateOnly? End)
{
    /// <summary>
    /// Returns true when the two half-open ranges share a day.
    /// </summary>
    /// <param name="other">The other appointment.</param>
    /// <returns>True when the ranges overlap.</returns>
    public bool Overlaps(DateRange other)
    {
        var thisEnd = End ?? DateOnly.MaxValue;
        var otherEnd = other.End ?? DateOnly.MaxValue;
        return Start < otherEnd && other.Start < thisEnd;
    }

    /// <summary>
    /// Returns true when <paramref name="candidate"/> overlaps any existing appointment.
    /// </summary>
    /// <param name="candidate">The range being saved.</param>
    /// <param name="existing">Appointments already stored for that lecturer.</param>
    /// <returns>True when the candidate must be rejected.</returns>
    public static bool OverlapsAny(DateRange candidate, IEnumerable<DateRange> existing)
    {
        ArgumentNullException.ThrowIfNull(existing);
        return existing.Any(candidate.Overlaps);
    }
}

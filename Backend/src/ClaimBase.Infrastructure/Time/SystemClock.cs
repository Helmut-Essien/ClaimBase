using ClaimBase.Application.Common.Interfaces;

namespace ClaimBase.Infrastructure.Time;

/// <summary>UTC clock backed by <see cref="DateTimeOffset.UtcNow"/>.</summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

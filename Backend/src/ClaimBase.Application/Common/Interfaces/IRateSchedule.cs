using ClaimBase.Domain.Rates;
using ClaimBase.Shared.Common;
using ClaimBase.Shared.Rates;

namespace ClaimBase.Application.Common.Interfaces;

/// <summary>
/// Teaching and transport rates for the current tenant. Reads are filtered to that tenant. Writes share one <c>SaveChanges</c> per use case.
/// </summary>
public interface IRateSchedule
{
    /// <summary>
    /// Lists teaching rates, optionally for one title, qualification, or calendar day, ordered by title, qualification, then start date.
    /// <paramref name="on"/> keeps only rows whose half-open range contains that day. A day with no row is absent, not zero.
    /// </summary>
    Task<PagedResult<TeachingRateResponse>> ListTeachingAsync(
        int page,
        int pageSize,
        string? positionTitleId,
        string? qualificationId,
        DateOnly? on,
        CancellationToken cancellationToken);

    /// <summary>Loads the rates for one title and qualification so a replacement can close an open-ended row.</summary>
    Task<IReadOnlyList<TeachingRate>> ListTeachingForUpdateAsync(
        string positionTitleId,
        string qualificationId,
        CancellationToken cancellationToken);

    /// <summary>True when the position title is in this tenant.</summary>
    Task<bool> PositionTitleExistsAsync(string id, CancellationToken cancellationToken);

    /// <summary>True when the qualification is in this tenant.</summary>
    Task<bool> QualificationExistsAsync(string id, CancellationToken cancellationToken);

    /// <summary>Stages a teaching rate.</summary>
    void Add(TeachingRate rate);

    /// <summary>Loads one teaching rate with its title and qualification names.</summary>
    Task<TeachingRateResponse?> GetTeachingAsync(string id, CancellationToken cancellationToken);

    /// <summary>Lists transport rates, oldest start date first.</summary>
    Task<PagedResult<TransportRateResponse>> ListTransportAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Loads the tenant transport timeline so a replacement can close an open-ended row.</summary>
    Task<IReadOnlyList<TransportRate>> ListTransportForUpdateAsync(CancellationToken cancellationToken);

    /// <summary>Stages a transport rate.</summary>
    void Add(TransportRate rate);

    /// <summary>Saves the staged changes. A unique-index violation becomes a conflict.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Runs <paramref name="work"/> while one rate timeline is locked.
    /// The overlap check and the save that follows must both run inside this lock, or two creates can store overlapping dates.
    /// </summary>
    /// <typeparam name="T">The value <paramref name="work"/> returns.</typeparam>
    /// <param name="timelineKey">Stable key for one teaching cell or the transport timeline. It is not a rate amount.</param>
    /// <param name="work">Overlap check, replacement, and save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The value returned by <paramref name="work"/>.</returns>
    Task<T> LockTimelineAsync<T>(string timelineKey, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken);
}

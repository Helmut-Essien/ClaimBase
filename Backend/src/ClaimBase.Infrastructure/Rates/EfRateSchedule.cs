using ClaimBase.Application.Common.Exceptions;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Rates;
using ClaimBase.Infrastructure.Persistence;
using ClaimBase.Shared.Common;
using ClaimBase.Shared.Rates;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ClaimBase.Infrastructure.Rates;

/// <summary>EF Core rate store. Lists project in the database and page there.</summary>
public sealed class EfRateSchedule : IRateSchedule
{
    private readonly AppDbContext _db;

    /// <summary>
    /// Creates the store.
    /// </summary>
    /// <param name="db">Request database context. The tenant filter is already applied.</param>
    public EfRateSchedule(AppDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <inheritdoc />
    public Task<PagedResult<TeachingRateResponse>> ListTeachingAsync(
        int page,
        int pageSize,
        string? positionTitleId,
        string? qualificationId,
        DateOnly? on,
        CancellationToken cancellationToken)
    {
        var rates = _db.TeachingRates.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(positionTitleId))
            rates = rates.Where(rate => rate.PositionTitleId == positionTitleId);
        if (!string.IsNullOrWhiteSpace(qualificationId))
            rates = rates.Where(rate => rate.QualificationId == qualificationId);
        if (on is not null)
        {
            var day = on.Value;
            // Half-open [EffectiveFrom, EffectiveTo). The end day is not in force, and a null end still is.
            rates = rates.Where(rate => rate.EffectiveFrom <= day && (rate.EffectiveTo == null || rate.EffectiveTo > day));
        }

        var query =
            from rate in rates
            join title in _db.PositionTitles.AsNoTracking() on rate.PositionTitleId equals title.Id
            join qualification in _db.Qualifications.AsNoTracking() on rate.QualificationId equals qualification.Id
            orderby title.Name, qualification.Name, rate.EffectiveFrom
            select new TeachingRateResponse
            {
                Id = rate.Id,
                PositionTitleId = title.Id,
                PositionTitleName = title.Name,
                QualificationId = qualification.Id,
                QualificationName = qualification.Name,
                Amount = rate.Amount,
                EffectiveFrom = rate.EffectiveFrom,
                EffectiveTo = rate.EffectiveTo
            };

        return PageAsync(query, page, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TeachingRate>> ListTeachingForUpdateAsync(
        string positionTitleId,
        string qualificationId,
        CancellationToken cancellationToken)
    {
        return await _db.TeachingRates
            .Where(rate => rate.PositionTitleId == positionTitleId && rate.QualificationId == qualificationId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> PositionTitleExistsAsync(string id, CancellationToken cancellationToken) =>
        _db.PositionTitles.AnyAsync(title => title.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> QualificationExistsAsync(string id, CancellationToken cancellationToken) =>
        _db.Qualifications.AnyAsync(qualification => qualification.Id == id, cancellationToken);

    /// <inheritdoc />
    public void Add(TeachingRate rate) => _db.TeachingRates.Add(rate);

    /// <inheritdoc />
    public Task<TeachingRateResponse?> GetTeachingAsync(string id, CancellationToken cancellationToken) =>
        (
            from rate in _db.TeachingRates.AsNoTracking()
            join title in _db.PositionTitles.AsNoTracking() on rate.PositionTitleId equals title.Id
            join qualification in _db.Qualifications.AsNoTracking() on rate.QualificationId equals qualification.Id
            where rate.Id == id
            select new TeachingRateResponse
            {
                Id = rate.Id,
                PositionTitleId = title.Id,
                PositionTitleName = title.Name,
                QualificationId = qualification.Id,
                QualificationName = qualification.Name,
                Amount = rate.Amount,
                EffectiveFrom = rate.EffectiveFrom,
                EffectiveTo = rate.EffectiveTo
            }).FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public Task<PagedResult<TransportRateResponse>> ListTransportAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        PageAsync(
            _db.TransportRates.AsNoTracking()
                .OrderBy(rate => rate.EffectiveFrom)
                .Select(rate => new TransportRateResponse
                {
                    Id = rate.Id,
                    Amount = rate.Amount,
                    EffectiveFrom = rate.EffectiveFrom,
                    EffectiveTo = rate.EffectiveTo
                }),
            page,
            pageSize,
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransportRate>> ListTransportForUpdateAsync(CancellationToken cancellationToken) =>
        await _db.TransportRates.ToListAsync(cancellationToken);

    /// <inheritdoc />
    public void Add(TransportRate rate) => _db.TransportRates.Add(rate);

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflictAppException("That value is already in use.");
        }
    }

    /// <inheritdoc />
    public async Task<T> LockTimelineAsync<T>(string timelineKey, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timelineKey);
        ArgumentNullException.ThrowIfNull(work);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        // The lock lasts until this transaction ends. A second create for the same key waits, then reads the committed rows.
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({timelineKey}, {0L}))",
            cancellationToken);
        var result = await work(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static async Task<PagedResult<T>> PageAsync<T>(IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }
}

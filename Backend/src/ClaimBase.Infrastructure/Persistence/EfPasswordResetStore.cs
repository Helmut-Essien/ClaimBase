using ClaimBase.Application.Common;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClaimBase.Infrastructure.Persistence;

/// <summary>EF store for reset links. These queries ignore the tenant filter because the caller is anonymous.</summary>
public sealed class EfPasswordResetStore : IPasswordResetStore
{
    private readonly AppDbContext _db;

    /// <summary>
    /// Creates the store.
    /// </summary>
    /// <param name="db">The request database context.</param>
    public EfPasswordResetStore(AppDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PasswordResetAccount?> FindSingleAccountByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        // Login uses the same rule: an address shared by two universities is not a guess.
        var matches = await _db.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(user => user.Email == email)
            .Select(user => new PasswordResetAccount(user.Id, user.TenantId, user.Email))
            .ToListAsync(cancellationToken);

        return matches.Count == 1 ? matches[0] : null;
    }

    /// <inheritdoc />
    public async Task IssueTokenAsync(
        PasswordResetAccount account,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await LockUserAsync(account.UserId, cancellationToken);

        var outstanding = await _db.PasswordResetTokens
            .IgnoreQueryFilters()
            .Where(token => token.UserId == account.UserId && token.UsedAt == null)
            .ToListAsync(cancellationToken);

        // A new request replaces the previous link. The lock keeps two requests from leaving two live links.
        // Save the consumed rows before the insert so the one-live-link index does not see both at once.
        foreach (var previous in outstanding)
            previous.Consume(createdAt);

        if (outstanding.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        _db.PasswordResetTokens.Add(PasswordResetToken.Issue(
            EntityIds.New(),
            account.TenantId,
            account.UserId,
            tokenHash,
            expiresAt,
            createdAt));
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> TryResetPasswordAsync(
        string email,
        string tokenHash,
        string newPasswordHash,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var ownerId = await _db.PasswordResetTokens
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(row => row.TokenHash == tokenHash && row.UsedAt == null && row.ExpiresAt > usedAt)
            .Select(row => row.UserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (ownerId is null)
            return false;

        // Same lock as issuing a link, so a reset and a new request cannot both leave the link usable.
        await LockUserAsync(ownerId, cancellationToken);

        var token = await _db.PasswordResetTokens
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                row => row.TokenHash == tokenHash && row.UsedAt == null && row.ExpiresAt > usedAt,
                cancellationToken);

        if (token is null)
            return false;

        var user = await _db.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(row => row.Id == token.UserId, cancellationToken);

        // A wrong email must not burn a link that still belongs to the address on the message.
        if (user is null || !string.Equals(user.Email, email, StringComparison.Ordinal))
            return false;

        user.ChangePassword(newPasswordHash, usedAt);
        token.Consume(usedAt);

        var others = await _db.PasswordResetTokens
            .IgnoreQueryFilters()
            .Where(row => row.UserId == user.Id && row.Id != token.Id && row.UsedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var other in others)
            other.Consume(usedAt);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Holds a transaction-scoped lock for one user. The second reset waits, then sees the link already used.
    /// </summary>
    private Task LockUserAsync(string userId, CancellationToken cancellationToken) =>
        _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({userId}, 0))",
            cancellationToken);
}

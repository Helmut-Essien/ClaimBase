using System.Globalization;
using ClaimBase.Application.Common;
using ClaimBase.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimBase.Infrastructure.Identity;

/// <summary>
/// Rejects an access token issued before the latest password reset.
/// ClaimBase has no refresh-token table, so this primary-key read is what ends the previous session.
/// </summary>
public static class PasswordChangedStampValidator
{
    /// <summary>
    /// Compares the <c>pwd</c> claim with <c>User.PasswordChangedAt</c>.
    /// Authentication runs before the tenant filter is set, so this read ignores that filter and checks the tenant claim itself.
    /// </summary>
    /// <param name="context">The validated token.</param>
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var principal = context.Principal;
        var userId = principal?.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            context.Fail("The access token has no subject.");
            return;
        }

        // A nested scope keeps this read off the request DbContext. That context captures the tenant id at
        // construction, and this event runs before the tenant middleware has set it.
        var scopes = context.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(row => row.Id == userId)
            .Select(row => new { row.TenantId, row.PasswordChangedAt })
            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

        if (user is null)
        {
            context.Fail("The access token subject is unknown.");
            return;
        }

        var tenantId = principal!.FindFirst("tenantId")?.Value;
        if (!string.Equals(user.TenantId, tenantId, StringComparison.Ordinal))
        {
            context.Fail("The access token tenant does not match the user.");
            return;
        }

        var expected = user.PasswordChangedAt?.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var actual = principal.FindFirst(AuthClaimTypes.PasswordChangedAt)?.Value;
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
            context.Fail("The access token was issued before the current password.");
    }
}

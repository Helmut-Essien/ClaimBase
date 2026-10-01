using ClaimBase.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimBase.Infrastructure.Persistence;

/// <summary>Development-only schema update and seed. Production and tests do not call this on boot.</summary>
public static class DevelopmentDatabase
{
    /// <summary>
    /// Applies migrations and inserts the development tenant when it is missing.
    /// </summary>
    /// <param name="services">The built service provider.</param>
    /// <param name="configuration">Host configuration. <c>Seed:Password</c> is required.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task MigrateAndSeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        await DevelopmentSeed.ApplyAsync(
            db,
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
            configuration,
            cancellationToken);
    }
}

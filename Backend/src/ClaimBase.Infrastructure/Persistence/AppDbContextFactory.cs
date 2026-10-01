using ClaimBase.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClaimBase.Infrastructure.Persistence;

/// <summary>
/// Builds the model for <c>dotnet ef</c>. The connection string is a placeholder; adding a migration does not open a database.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <inheritdoc />
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5434;Database=claimbase_db;Username=claimbase;Password=unused")
            .Options;

        return new AppDbContext(options, new CurrentTenant());
    }
}

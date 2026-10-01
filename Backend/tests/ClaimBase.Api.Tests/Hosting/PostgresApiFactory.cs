using ClaimBase.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ClaimBase.Api.Tests.Hosting;

/// <summary>One PostgreSQL container for the identity HTTP and tenant-filter tests.</summary>
public sealed class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _postgres.StartAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // The Testing appsettings connection string points at the dev port. Replace the context after the host reads that file.
        builder.ConfigureTestServices(services =>
        {
            var options = services.Single(service => service.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(options);
            var context = services.Single(service => service.ServiceType == typeof(AppDbContext));
            services.Remove(context);
            services.AddDbContext<AppDbContext>(db => db.UseNpgsql(_postgres.GetConnectionString()));
        });
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresApiFactory>
{
    public const string Name = "postgres";
}

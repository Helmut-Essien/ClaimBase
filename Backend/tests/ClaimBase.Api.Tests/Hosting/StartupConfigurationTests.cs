using ClaimBase.Infrastructure.Hosting;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ClaimBase.Api.Tests.Hosting;

public class StartupConfigurationTests
{
    [Fact]
    public void Validate_WhenDevelopmentUsesCommittedDefaults_DoesNotThrow()
    {
        var configuration = Configuration(
            "Host=localhost;Port=5434;Database=claimbase_db;Username=claimbase;Password=claimbase_dev",
            StartupConfiguration.DevelopmentJwtKey);

        var act = () => StartupConfiguration.Validate(configuration, "Development");

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenProductionConnectionStringIsMissing_Throws()
    {
        var configuration = Configuration(" ", new string('k', 64));

        var act = () => StartupConfiguration.Validate(configuration, "Production");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DefaultConnection*");
    }

    [Fact]
    public void Validate_WhenProductionUsesDevelopmentPassword_Throws()
    {
        var configuration = Configuration(
            "Host=db;Database=claimbase_db;Username=claimbase;Password=claimbase_dev",
            new string('k', 64));

        var act = () => StartupConfiguration.Validate(configuration, "Production");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Development Postgres password*");
    }

    [Fact]
    public void Validate_WhenProductionEnablesErrorDetail_Throws()
    {
        var configuration = Configuration(
            "Host=db;Database=claimbase_db;Username=claimbase;Password=secret;Include Error Detail=true",
            new string('k', 64));

        var act = () => StartupConfiguration.Validate(configuration, "Production");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Include Error Detail*");
    }

    [Fact]
    public void Validate_WhenProductionJwtKeyIsShorterThan64_Throws()
    {
        var configuration = Configuration(
            "Host=db;Database=claimbase_db;Username=claimbase;Password=secret",
            new string('k', 40));

        var act = () => StartupConfiguration.Validate(configuration, "Production");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*64*");
    }

    [Fact]
    public void Validate_WhenProductionUsesDevelopmentJwtKey_Throws()
    {
        var configuration = Configuration(
            "Host=db;Database=claimbase_db;Username=claimbase;Password=secret",
            StartupConfiguration.DevelopmentJwtKey);

        var act = () => StartupConfiguration.Validate(configuration, "Production");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Development signing key*");
    }

    private static IConfiguration Configuration(string connectionString, string jwtKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Jwt:Key"] = jwtKey
            })
            .Build();
}

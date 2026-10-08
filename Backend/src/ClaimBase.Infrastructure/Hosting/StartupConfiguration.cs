using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ClaimBase.Infrastructure.Hosting;

/// <summary>
/// Fail-fast checks so Production cannot boot with the Development database password, a leaky connection string, or the Development JWT key.
/// </summary>
public static class StartupConfiguration
{
    /// <summary>Committed Development JWT signing key. Production must override it with <c>JWT__KEY</c>.</summary>
    public const string DevelopmentJwtKey = "ClaimBase_Dev_Jwt_Signing_Key_Not_For_Production_0123456789abcdef";

    /// <summary>Minimum HMAC key length in every environment.</summary>
    public const int MinimumJwtKeyLength = 32;

    /// <summary>Production HMAC key length required by the ClaimBase skill.</summary>
    public const int ProductionJwtKeyLength = 64;

    /// <summary>Development Postgres password. Production must not reuse it.</summary>
    public const string DevelopmentDatabasePassword = "claimbase_dev";

    /// <summary>
    /// Validates the connection string, JWT key, and, in Production, the portal URL and SMTP host used by password reset.
    /// </summary>
    /// <param name="configuration">The host configuration.</param>
    /// <param name="environmentName">The ASP.NET environment name.</param>
    /// <exception cref="InvalidOperationException">A required setting is missing or is a known Development secret in Production.</exception>
    public static void Validate(IConfiguration configuration, string environmentName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var jwtKey = configuration["Jwt:Key"];
        var isProduction = string.Equals(environmentName, Environments.Production, StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < MinimumJwtKeyLength)
            throw new InvalidOperationException($"Jwt:Key must be at least {MinimumJwtKeyLength} characters.");

        if (!isProduction)
            return;

        if (jwtKey.Length < ProductionJwtKeyLength)
            throw new InvalidOperationException($"Jwt:Key must be at least {ProductionJwtKeyLength} characters in Production.");

        if (string.Equals(jwtKey, DevelopmentJwtKey, StringComparison.Ordinal))
            throw new InvalidOperationException("Jwt:Key is the Development signing key. Set JWT__KEY to a unique Production secret.");

        if (connectionString.Contains($"Password={DevelopmentDatabasePassword}", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection still uses the Development Postgres password.");

        if (connectionString.Contains("Include Error Detail", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection must not enable Include Error Detail in Production.");

        var portalBaseUrl = configuration["Portal:BaseUrl"];
        if (!Uri.TryCreate(portalBaseUrl, UriKind.Absolute, out var portalUri) || portalUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Portal:BaseUrl must be an absolute https URL in Production.");

        if (string.IsNullOrWhiteSpace(configuration["Email:Host"]))
            throw new InvalidOperationException("Email:Host is required in Production.");

        if (string.IsNullOrWhiteSpace(configuration["Email:FromAddress"]))
            throw new InvalidOperationException("Email:FromAddress is required in Production.");
    }
}

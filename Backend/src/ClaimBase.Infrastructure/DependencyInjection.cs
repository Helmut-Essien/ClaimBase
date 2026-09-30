using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimBase.Infrastructure;

/// <summary>
/// Registers infrastructure adapters. Persistence is added with the first business entity.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services that exist in this slice.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Host configuration. Reserved for the connection string when the model exists.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // The tenant query filter is registered on the EF model when the first TenantId entity is mapped.
        // Hangfire is registered with the biometric import job, and each job receives TenantId as an argument.
        return services;
    }
}

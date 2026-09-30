using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimBase.Application;

/// <summary>
/// Registers application use cases. Feature handlers are added in later slices.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds MediatR and FluentValidation from this assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}

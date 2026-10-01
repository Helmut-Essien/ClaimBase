using ClaimBase.Application.Common;
using ClaimBase.Application.Common.Behaviors;
using ClaimBase.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimBase.Application;

/// <summary>Registers application use cases.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds MediatR, FluentValidation, and the validation pipeline from this assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<ISetupAccess, SetupAccess>();
        return services;
    }
}

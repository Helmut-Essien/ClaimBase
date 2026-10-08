using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;

namespace ClaimBase.Api.Hosting;

/// <summary>Limits login, forgot-password, and reset-password without slowing <c>GET /api/auth/me</c>.</summary>
public static class LoginRateLimiter
{
    /// <summary>Policy name used by the login action.</summary>
    public const string PolicyName = "login";

    /// <summary>
    /// Adds a fixed window of 10 attempts per minute per client IP, and the same window per email.
    /// Login, forgot-password, and reset-password share both windows.
    /// The Testing host raises both windows so the suite can sign in for every case.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="environment">Host environment.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddLoginRateLimiter(this IServiceCollection services, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);

        var permitLimit = environment.IsEnvironment("Testing") ? 10_000 : 10;
        services.AddSingleton(new LoginEmailLimiter(permitLimit));
        services.AddScoped<LoginEmailRateLimitFilter>();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(PolicyName, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }
}

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ClaimBase.Api.Hosting;

/// <summary>Limits password attempts without slowing <c>GET /api/auth/me</c>.</summary>
public static class LoginRateLimiter
{
    /// <summary>Policy name used by the login action.</summary>
    public const string PolicyName = "login";

    /// <summary>
    /// Adds a fixed window of 10 login attempts per minute per client IP.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddLoginRateLimiter(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(PolicyName, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }
}

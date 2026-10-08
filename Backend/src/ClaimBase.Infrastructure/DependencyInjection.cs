using System.Text;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Infrastructure.Academic;
using ClaimBase.Infrastructure.Identity;
using ClaimBase.Infrastructure.Rates;
using ClaimBase.Infrastructure.Persistence;
using ClaimBase.Infrastructure.Tenancy;
using ClaimBase.Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ClaimBase.Infrastructure;

/// <summary>Registers persistence, password hashing, password reset mail, and JWT authentication.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the database, the tenant filter, and authentication.
    /// Hangfire stays unregistered until the biometric import job exists. That job must not change claim amounts.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Host configuration.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<CurrentTenant>();
        services.AddScoped<ICurrentTenant>(provider => provider.GetRequiredService<CurrentTenant>());
        services.AddScoped<IIdentityReader, EfIdentityReader>();
        services.AddScoped<IPasswordResetStore, EfPasswordResetStore>();
        services.AddSingleton<IResetTokenProtector, ResetTokenProtector>();
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<PortalOptions>(configuration.GetSection(PortalOptions.SectionName));
        services.AddSingleton<IPortalLinks, PortalLinkBuilder>();
        services.AddSingleton<PasswordResetEmailLog>();
        services.AddSingleton<IPasswordResetEmailLog>(provider => provider.GetRequiredService<PasswordResetEmailLog>());
        services.AddSingleton<PasswordResetEmailQueue>();
        services.AddSingleton<IPasswordResetEmailQueue>(provider => provider.GetRequiredService<PasswordResetEmailQueue>());
        services.AddHostedService(provider => provider.GetRequiredService<PasswordResetEmailQueue>());
        services.AddScoped<SmtpPasswordResetSender>();
        services.AddScoped<IPasswordResetDelivery, PasswordResetDelivery>();
        services.AddScoped<IPasswordResetMailer, PasswordResetMailer>();
        services.AddScoped<IAcademicCatalog, EfAcademicCatalog>();
        services.AddScoped<IRateSchedule, EfRateSchedule>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IClock, SystemClock>();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };

                // One primary-key read. A reset must end tokens issued before the new password, and there is no refresh-token table to revoke.
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = PasswordChangedStampValidator.ValidateAsync
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        return services;
    }
}

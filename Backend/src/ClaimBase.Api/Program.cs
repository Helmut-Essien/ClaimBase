using ClaimBase.Api.Hosting;
using ClaimBase.Api.Logging;
using ClaimBase.Api.Middleware;
using ClaimBase.Application;
using ClaimBase.Infrastructure;
using ClaimBase.Infrastructure.Hosting;
using ClaimBase.Infrastructure.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    StartupConfiguration.Validate(builder.Configuration, builder.Environment.EnvironmentName);
    var forwardedHeaders = ForwardedClientHeaders.Create(builder.Configuration);

    builder.WebHost.ConfigureKestrel(options =>
    {
        // JSON payloads stay small until the biometric workbook upload exists.
        options.Limits.MaxRequestBodySize = 128 * 1024;
    });

    builder.Host.UseSerilog((context, logger) =>
    {
        logger
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Destructure.With<SecretRedactingPolicy>();

        if (context.HostingEnvironment.IsEnvironment("Testing"))
        {
            logger.MinimumLevel.Warning();
            return;
        }

        logger.WriteTo.Console();

        // File sink is local-dev convenience. Production hosts collect stdout.
        if (context.HostingEnvironment.IsDevelopment())
        {
            logger.WriteTo.File("logs/claimbase-.log", rollingInterval: RollingInterval.Day);
        }
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddLoginRateLimiter(builder.Environment);
    builder.Services.AddControllers();
    builder.Services.AddHealthChecks();
    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
    });

    if (builder.Environment.IsDevelopment())
        builder.Services.AddOpenApi();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Portal", policy =>
        {
            var origins = CorsOrigins.Resolve(builder.Configuration);
            if (origins.Length == 0)
            {
                // Same-origin Portal behind a reverse proxy: refuse every browser cross-origin call.
                policy.SetIsOriginAllowed(_ => false);
                return;
            }

            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    if (app.Environment.IsProduction())
    {
        // Known proxy lists stay populated. Clearing both of them would trust every X-Forwarded-For.
        app.UseForwardedHeaders(forwardedHeaders);
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    if (app.Environment.IsDevelopment())
    {
        // Production applies migrations during deploy. Tests apply them in the fixture. Development applies them on boot.
        await DevelopmentDatabase.MigrateAndSeedAsync(app.Services, app.Configuration, CancellationToken.None);
        app.MapOpenApi().AllowAnonymous();
    }

    // CORS runs before error handling so failure responses still include Allow-Origin.
    app.UseCors("Portal");
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.UseMiddleware<CurrentTenantMiddleware>();
    app.MapControllers();
    app.MapHealthChecks("/health").AllowAnonymous();

    app.Run();
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Entry point partial so API tests can host this assembly with <c>WebApplicationFactory</c>.</summary>
public partial class Program;

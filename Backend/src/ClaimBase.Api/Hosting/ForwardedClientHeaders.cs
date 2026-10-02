using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ClaimBase.Api.Hosting;

/// <summary>
/// Builds forwarded-header options that keep the known-proxy check on.
/// Loopback stays trusted, and operators add the real reverse proxy with configuration.
/// </summary>
public static class ForwardedClientHeaders
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "ForwardedHeaders";

    /// <summary>
    /// Creates options for <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c>.
    /// One hop is accepted, and only from loopback or a configured proxy.
    /// </summary>
    /// <param name="configuration">Host configuration.</param>
    /// <returns>Options whose known-proxy lists are never both empty.</returns>
    /// <exception cref="InvalidOperationException">A configured proxy or network is not a valid address or CIDR.</exception>
    public static ForwardedHeadersOptions Create(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1
        };

        // Both lists must stay non-empty. ASP.NET Core skips the known-proxy check when they are cleared,
        // and then any client can pick a new RemoteIpAddress with X-Forwarded-For.
        var section = configuration.GetSection(SectionName);
        AddProxies(options, section.GetSection("KnownProxies").Get<string[]>());
        AddNetworks(options, section.GetSection("KnownNetworks").Get<string[]>());

        return options;
    }

    /// <summary>
    /// Applies <see cref="Create"/> to one connection. An untrusted peer keeps <paramref name="remoteAddress"/>.
    /// </summary>
    /// <param name="configuration">Host configuration.</param>
    /// <param name="remoteAddress">TCP peer address.</param>
    /// <param name="forwardedFor">Raw <c>X-Forwarded-For</c> value, or null when the header is absent.</param>
    /// <returns>The address the rest of the pipeline would see.</returns>
    public static async Task<IPAddress?> RemoteAddressAfterForwardingAsync(
        IConfiguration configuration,
        IPAddress remoteAddress,
        string? forwardedFor)
    {
        ArgumentNullException.ThrowIfNull(remoteAddress);

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = remoteAddress;
        if (forwardedFor is not null)
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;

        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(Create(configuration)));
        await middleware.Invoke(context);
        return context.Connection.RemoteIpAddress;
    }

    private static void AddProxies(ForwardedHeadersOptions options, string[]? proxies)
    {
        if (proxies is null)
            return;

        foreach (var proxy in proxies)
        {
            if (string.IsNullOrWhiteSpace(proxy) || !IPAddress.TryParse(proxy.Trim(), out var address))
                throw new InvalidOperationException("ForwardedHeaders:KnownProxies contains an entry that is not an IP address.");

            options.KnownProxies.Add(address);
        }
    }

    private static void AddNetworks(ForwardedHeadersOptions options, string[]? networks)
    {
        if (networks is null)
            return;

        foreach (var network in networks)
        {
            if (string.IsNullOrWhiteSpace(network) || !Microsoft.AspNetCore.HttpOverrides.IPNetwork.TryParse(network.Trim(), out var parsed))
                throw new InvalidOperationException("ForwardedHeaders:KnownNetworks contains an entry that is not a CIDR block.");

            options.KnownNetworks.Add(parsed);
        }
    }
}

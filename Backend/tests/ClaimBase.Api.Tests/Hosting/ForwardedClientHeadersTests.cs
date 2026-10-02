using System.Net;
using ClaimBase.Api.Hosting;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ClaimBase.Api.Tests.Hosting;

public class ForwardedClientHeadersTests
{
    [Fact]
    public async Task UntrustedPeer_KeepsItsConnectionAddress_WhenForwardedForIsSpoofed()
    {
        var seen = await ForwardedClientHeaders.RemoteAddressAfterForwardingAsync(
            new ConfigurationBuilder().Build(),
            IPAddress.Parse("203.0.113.8"),
            "198.51.100.20");

        seen.Should().Be(IPAddress.Parse("203.0.113.8"));
    }

    [Fact]
    public async Task LoopbackProxy_AppliesOneForwardedAddress()
    {
        var seen = await ForwardedClientHeaders.RemoteAddressAfterForwardingAsync(
            new ConfigurationBuilder().Build(),
            IPAddress.Loopback,
            "198.51.100.20");

        seen.Should().Be(IPAddress.Parse("198.51.100.20"));
    }

    [Fact]
    public void Create_AddsAConfiguredProxy_AndKeepsLoopback()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:KnownProxies:0"] = "10.1.2.3",
                ["ForwardedHeaders:KnownNetworks:0"] = "10.0.0.0/8"
            })
            .Build();

        var options = ForwardedClientHeaders.Create(configuration);

        options.KnownProxies.Should().Contain(IPAddress.IPv6Loopback);
        options.KnownProxies.Should().Contain(IPAddress.Parse("10.1.2.3"));
        options.KnownNetworks.Should().Contain(network => network.Contains(IPAddress.Loopback));
        options.KnownNetworks.Should().Contain(network => network.Contains(IPAddress.Parse("10.1.0.1")));
        options.ForwardLimit.Should().Be(1);
    }

    [Fact]
    public void Create_WhenAProxyIsNotAnIp_Throws()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:KnownProxies:0"] = "not-an-ip"
            })
            .Build();

        var act = () => ForwardedClientHeaders.Create(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*KnownProxies*");
    }
}

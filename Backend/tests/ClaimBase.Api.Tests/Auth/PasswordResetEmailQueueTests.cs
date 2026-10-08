using ClaimBase.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ClaimBase.Api.Tests.Auth;

public class PasswordResetEmailQueueTests
{
    [Fact]
    public async Task Stop_CompletesAnIdleReader_WithoutFaulting()
    {
        var queue = new PasswordResetEmailQueue(
            Substitute.For<IServiceScopeFactory>(),
            NullLogger<PasswordResetEmailQueue>.Instance);

        await queue.StartAsync(CancellationToken.None);

        var stop = queue.StopAsync(CancellationToken.None);
        var finished = await Task.WhenAny(stop, Task.Delay(TimeSpan.FromSeconds(2)));
        finished.Should().Be(stop, "host shutdown must not leave the reader blocked");
        await stop;

        queue.ExecuteTask.Should().NotBeNull();
        queue.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
    }
}

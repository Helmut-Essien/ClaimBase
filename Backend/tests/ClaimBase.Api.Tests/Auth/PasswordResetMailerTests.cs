using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Infrastructure.Identity;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace ClaimBase.Api.Tests.Auth;

public class PasswordResetMailerTests
{
    [Fact]
    public async Task Production_EnqueuesAMiss_WithoutWritingAToken()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        var queue = Substitute.For<IPasswordResetEmailQueue>();
        var delivery = Substitute.For<IPasswordResetDelivery>();
        var mailer = new PasswordResetMailer(environment, queue, delivery);

        await mailer.QueueAsync("missing@claimbase.test", null, CancellationToken.None);

        await queue.Received(1).EnqueueAsync(
            Arg.Is<PasswordResetEmailWork>(work => work.Email == "missing@claimbase.test" && work.Account == null),
            Arg.Any<CancellationToken>());
        await delivery.DidNotReceive().DeliverAsync(Arg.Any<string>(), Arg.Any<PasswordResetAccount?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Testing_DeliversInline()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Testing");
        var queue = Substitute.For<IPasswordResetEmailQueue>();
        var delivery = Substitute.For<IPasswordResetDelivery>();
        var account = new PasswordResetAccount("user", "tenant", "ada@claimbase.test");
        var mailer = new PasswordResetMailer(environment, queue, delivery);

        await mailer.QueueAsync(account.Email, account, CancellationToken.None);

        await delivery.Received(1).DeliverAsync(account.Email, account, Arg.Any<CancellationToken>());
        await queue.DidNotReceive().EnqueueAsync(Arg.Any<PasswordResetEmailWork>(), Arg.Any<CancellationToken>());
    }
}

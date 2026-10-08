using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Application.Features.Auth;
using ClaimBase.Shared.Auth;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace ClaimBase.Api.Tests.Auth;

public class ForgotPasswordCommandHandlerTests
{
    [Fact]
    public async Task UnknownEmail_ReturnsTheSameMessage_AndStillQueues()
    {
        var store = Substitute.For<IPasswordResetStore>();
        store.FindSingleAccountByEmailAsync("missing@claimbase.test", Arg.Any<CancellationToken>())
            .Returns((PasswordResetAccount?)null);
        var mailer = Substitute.For<IPasswordResetMailer>();
        var handler = new ForgotPasswordCommandHandler(store, mailer);

        var response = await handler.Handle(new ForgotPasswordCommand(" Missing@ClaimBase.test "), CancellationToken.None);

        response.Message.Should().Be(PasswordResetCopy.LinkSent);
        await store.DidNotReceive().IssueTokenAsync(
            Arg.Any<PasswordResetAccount>(),
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
        await mailer.Received(1).QueueAsync("missing@claimbase.test", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SharedEmail_QueuesANullAccount()
    {
        var store = Substitute.For<IPasswordResetStore>();
        store.FindSingleAccountByEmailAsync("shared@claimbase.test", Arg.Any<CancellationToken>())
            .Returns((PasswordResetAccount?)null);
        var mailer = Substitute.For<IPasswordResetMailer>();
        var handler = new ForgotPasswordCommandHandler(store, mailer);

        var response = await handler.Handle(new ForgotPasswordCommand("shared@claimbase.test"), CancellationToken.None);

        response.Message.Should().Be(PasswordResetCopy.LinkSent);
        await mailer.Received(1).QueueAsync("shared@claimbase.test", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SingleAccount_QueuesThatAccount_WithoutWritingTheToken()
    {
        var account = new PasswordResetAccount("user", "tenant", "ada@claimbase.test");
        var store = Substitute.For<IPasswordResetStore>();
        store.FindSingleAccountByEmailAsync(account.Email, Arg.Any<CancellationToken>()).Returns(account);
        var mailer = Substitute.For<IPasswordResetMailer>();
        var handler = new ForgotPasswordCommandHandler(store, mailer);

        var response = await handler.Handle(new ForgotPasswordCommand(" Ada@ClaimBase.test "), CancellationToken.None);

        response.Message.Should().Be(PasswordResetCopy.LinkSent);
        await store.DidNotReceive().IssueTokenAsync(
            Arg.Any<PasswordResetAccount>(),
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
        await mailer.Received(1).QueueAsync(account.Email, account, Arg.Any<CancellationToken>());
    }
}

public class ResetPasswordCommandHandlerTests
{
    [Fact]
    public async Task InvalidLink_IsAValidationError()
    {
        var store = Substitute.For<IPasswordResetStore>();
        store.TryResetPasswordAsync(
                "ada@claimbase.test",
                "hashed",
                "new-hash",
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        var tokens = Substitute.For<IResetTokenProtector>();
        tokens.Hash("raw-token").Returns("hashed");
        var passwords = Substitute.For<IPasswordHasher>();
        passwords.Hash("new-password").Returns("new-hash");
        var handler = new ResetPasswordCommandHandler(store, tokens, passwords, Clock());

        var act = () => handler.Handle(
            new ResetPasswordCommand(" Ada@ClaimBase.test ", "raw-token", "new-password", "new-password"),
            CancellationToken.None);

        var error = await act.Should().ThrowAsync<ValidationException>();
        error.Which.Errors.Should().ContainSingle(failure => failure.ErrorMessage == PasswordResetCopy.InvalidToken);
    }

    [Fact]
    public async Task ValidLink_ReturnsTheSuccessMessage()
    {
        var store = Substitute.For<IPasswordResetStore>();
        store.TryResetPasswordAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new ResetPasswordCommandHandler(
            store,
            Substitute.For<IResetTokenProtector>(),
            Substitute.For<IPasswordHasher>(),
            Clock());

        var response = await handler.Handle(
            new ResetPasswordCommand("ada@claimbase.test", "raw-token", "new-password", "new-password"),
            CancellationToken.None);

        response.Message.Should().Be(PasswordResetCopy.Reset);
    }

    private static IClock Clock()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.Zero));
        return clock;
    }
}

public class PasswordResetValidatorTests
{
    private readonly ForgotPasswordCommandValidator _forgot = new();
    private readonly ResetPasswordCommandValidator _reset = new();

    [Fact]
    public void ForgotPassword_RejectsAnEmptyEmail()
    {
        var result = _forgot.Validate(new ForgotPasswordCommand(" "));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ResetPassword_RejectsAShortPassword()
    {
        var result = _reset.Validate(new ResetPasswordCommand("ada@claimbase.test", "token", "short", "short"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ResetPassword_RejectsAMismatch()
    {
        var result = _reset.Validate(new ResetPasswordCommand("ada@claimbase.test", "token", "password1", "password2"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "Passwords do not match.");
    }
}

using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Application.Common.Exceptions;
using ClaimBase.Application.Features.Auth;
using ClaimBase.Domain.Identity;
using ClaimBase.Infrastructure.Tenancy;
using FluentAssertions;
using NSubstitute;

namespace ClaimBase.Api.Tests.Auth;

public class LoginCommandHandlerTests
{
    private const string Password = "correct-password";

    [Fact]
    public async Task UnknownEmail_VerifiesADummyHash_AndDoesNotIssueAToken()
    {
        var users = Substitute.For<IIdentityReader>();
        users.FindByEmailIgnoringTenantAsync("missing@claimbase.test", Arg.Any<CancellationToken>())
            .Returns(new List<LoginCandidate>());
        var passwords = Substitute.For<IPasswordHasher>();
        var tokens = Substitute.For<IJwtTokenIssuer>();
        var handler = Handler(users, passwords, tokens);

        var act = () => handler.Handle(new LoginCommand(" missing@claimbase.test ", Password), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAppException>();
        passwords.Received(1).VerifyUnknownEmail(Password);
        tokens.DidNotReceive().Issue(Arg.Any<LoginCandidate>(), Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task EmailSharedByTwoTenants_DoesNotGuess()
    {
        var users = Substitute.For<IIdentityReader>();
        users.FindByEmailIgnoringTenantAsync("shared@claimbase.test", Arg.Any<CancellationToken>())
            .Returns(new List<LoginCandidate> { Candidate("a"), Candidate("b") });
        var passwords = Substitute.For<IPasswordHasher>();
        var tokens = Substitute.For<IJwtTokenIssuer>();
        var handler = Handler(users, passwords, tokens);

        var act = () => handler.Handle(new LoginCommand("shared@claimbase.test", Password), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAppException>();
        passwords.Received(1).VerifyUnknownEmail(Password);
        passwords.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
        tokens.DidNotReceive().Issue(Arg.Any<LoginCandidate>(), Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task WrongPassword_DoesNotIssueAToken()
    {
        var account = Candidate("one");
        var users = Substitute.For<IIdentityReader>();
        users.FindByEmailIgnoringTenantAsync(account.Email, Arg.Any<CancellationToken>())
            .Returns(new List<LoginCandidate> { account });
        var passwords = Substitute.For<IPasswordHasher>();
        passwords.Verify(Password, account.PasswordHash).Returns(false);
        var tokens = Substitute.For<IJwtTokenIssuer>();
        var handler = Handler(users, passwords, tokens);

        var act = () => handler.Handle(new LoginCommand(account.Email, Password), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAppException>();
        tokens.DidNotReceive().Issue(Arg.Any<LoginCandidate>(), Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task ValidPassword_ReturnsTheTokenAndTenant()
    {
        var account = Candidate("one");
        var users = Substitute.For<IIdentityReader>();
        users.FindByEmailIgnoringTenantAsync("admin@claimbase.test", Arg.Any<CancellationToken>())
            .Returns(new List<LoginCandidate> { account });
        var passwords = Substitute.For<IPasswordHasher>();
        passwords.Verify(Password, account.PasswordHash).Returns(true);
        var issuedAt = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
        var tokens = Substitute.For<IJwtTokenIssuer>();
        tokens.Issue(account, issuedAt).Returns(new IssuedToken("signed-token", issuedAt.AddHours(8)));
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(issuedAt);
        var handler = new LoginCommandHandler(users, passwords, tokens, clock);

        var response = await handler.Handle(new LoginCommand(" Admin@ClaimBase.test ", Password), CancellationToken.None);

        response.Token.Should().Be("signed-token");
        response.ExpiresAt.Should().Be(issuedAt.AddHours(8));
        response.TenantId.Should().Be(account.TenantId);
        response.Role.Should().Be(nameof(UserRole.Admin));
        response.Email.Should().Be(account.Email);
    }

    private static LoginCommandHandler Handler(IIdentityReader users, IPasswordHasher passwords, IJwtTokenIssuer tokens)
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(DateTimeOffset.UnixEpoch);
        return new LoginCommandHandler(users, passwords, tokens, clock);
    }

    private static LoginCandidate Candidate(string tenantId) =>
        new(
            "01JTEST0000000000000000001",
            tenantId,
            "admin@claimbase.test",
            "Ada Admin",
            "hash",
            UserRole.Admin,
            null,
            "University",
            "GHS");
}

public class GetMeQueryHandlerTests
{
    [Fact]
    public async Task UnresolvedTenant_IsUnauthorized()
    {
        var handler = new GetMeQueryHandler(Substitute.For<IIdentityReader>(), new CurrentTenant());

        var act = () => handler.Handle(new GetMeQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAppException>();
    }

    [Fact]
    public async Task MissingUser_IsNotFound()
    {
        var current = new CurrentTenant();
        current.Set("tenant-a", "user-a", UserRole.Admin, null);
        var users = Substitute.For<IIdentityReader>();
        users.FindPortalProfileAsync("user-a", Arg.Any<CancellationToken>()).Returns((PortalProfile?)null);
        var handler = new GetMeQueryHandler(users, current);

        var act = () => handler.Handle(new GetMeQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundAppException>();
    }

    [Fact]
    public async Task Lecturer_IsForbidden()
    {
        var current = new CurrentTenant();
        current.Set("tenant-a", "user-a", UserRole.Lecturer, null);
        var users = Substitute.For<IIdentityReader>();
        users.FindPortalProfileAsync("user-a", Arg.Any<CancellationToken>()).Returns(Profile(UserRole.Lecturer));
        var handler = new GetMeQueryHandler(users, current);

        var act = () => handler.Handle(new GetMeQuery(), CancellationToken.None);

        var error = await act.Should().ThrowAsync<ForbiddenAppException>();
        error.Which.Message.Should().Be("Use the ClaimBase mobile app.");
    }

    [Fact]
    public async Task PortalRole_ReturnsTheProfile()
    {
        var current = new CurrentTenant();
        current.Set("tenant-a", "user-a", UserRole.Finance, null);
        var users = Substitute.For<IIdentityReader>();
        users.FindPortalProfileAsync("user-a", Arg.Any<CancellationToken>()).Returns(Profile(UserRole.Finance));
        var handler = new GetMeQueryHandler(users, current);

        var response = await handler.Handle(new GetMeQuery(), CancellationToken.None);

        response.Role.Should().Be(nameof(UserRole.Finance));
        response.TimeZoneId.Should().Be("Africa/Accra");
        response.CurrencyCode.Should().Be("GHS");
    }

    private static PortalProfile Profile(UserRole role) =>
        new("user-a", "tenant-a", "person@claimbase.test", "Person", role, "University", "GHS", "Africa/Accra");
}

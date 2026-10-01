using ClaimBase.Application.Features.Auth;
using ClaimBase.Domain.Identity;
using ClaimBase.Shared.Auth;
using FluentAssertions;

namespace ClaimBase.Api.Tests.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void AcceptsANormalLogin()
    {
        var result = _validator.Validate(new LoginCommand("ada@claimbase.test", "password1"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void RejectsABadEmail(string email)
    {
        var result = _validator.Validate(new LoginCommand(email, "password1"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RejectsAnEmailOver320Characters()
    {
        var email = new string('a', UserConstraints.EmailMaxLength - 10) + "@school.test";
        email.Length.Should().BeGreaterThan(UserConstraints.EmailMaxLength);

        var result = _validator.Validate(new LoginCommand(email, "password1"));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("short")]
    [InlineData("")]
    public void RejectsAShortPassword(string password)
    {
        var result = _validator.Validate(new LoginCommand("ada@claimbase.test", password));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RejectsAPasswordOver128Characters()
    {
        var result = _validator.Validate(new LoginCommand("ada@claimbase.test", new string('p', 129)));

        result.IsValid.Should().BeFalse();
    }
}

public class AuthFieldLimitsTests
{
    [Fact]
    public void SharedLimitsMatchTheDomain()
    {
        AuthFieldLimits.Email.Should().Be(UserConstraints.EmailMaxLength);
        AuthFieldLimits.PasswordMin.Should().Be(UserConstraints.PasswordMinLength);
        AuthFieldLimits.PasswordMax.Should().Be(UserConstraints.PasswordMaxLength);
        AuthFieldLimits.DisplayName.Should().Be(UserConstraints.DisplayNameMaxLength);
        AuthFieldLimits.CurrencyCode.Should().Be(TenantConstraints.CurrencyCodeLength);
        AuthFieldLimits.TimeZoneId.Should().Be(TenantConstraints.TimeZoneIdMaxLength);
    }
}

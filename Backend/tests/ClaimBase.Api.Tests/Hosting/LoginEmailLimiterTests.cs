using ClaimBase.Api.Hosting;
using FluentAssertions;

namespace ClaimBase.Api.Tests.Hosting;

public class LoginEmailLimiterTests
{
    [Fact]
    public async Task TryConsume_AllowsTwoAttempts_ThenRejectsTheSameEmail()
    {
        using var limiter = new LoginEmailLimiter(2);

        (await limiter.TryConsumeAsync(" Person@ClaimBase.test ", CancellationToken.None)).Should().BeTrue();
        (await limiter.TryConsumeAsync("person@claimbase.test", CancellationToken.None)).Should().BeTrue();
        (await limiter.TryConsumeAsync("person@claimbase.test", CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task TryConsume_TracksEachEmailOnItsOwn()
    {
        using var limiter = new LoginEmailLimiter(1);

        (await limiter.TryConsumeAsync("one@claimbase.test", CancellationToken.None)).Should().BeTrue();
        (await limiter.TryConsumeAsync("two@claimbase.test", CancellationToken.None)).Should().BeTrue();
        (await limiter.TryConsumeAsync("one@claimbase.test", CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public void LoginAction_UsesTheEmailFilter()
    {
        var method = typeof(ClaimBase.Api.Controllers.AuthController).GetMethod("Login");

        method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ServiceFilterAttribute), inherit: false)
            .Cast<Microsoft.AspNetCore.Mvc.ServiceFilterAttribute>()
            .Should().Contain(attribute => attribute.ServiceType == typeof(LoginEmailRateLimitFilter));
    }

    [Theory]
    [InlineData("ForgotPassword")]
    [InlineData("ResetPassword")]
    public void PasswordResetActions_UseTheSameEmailFilter(string actionName)
    {
        var method = typeof(ClaimBase.Api.Controllers.AuthController).GetMethod(actionName);

        method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ServiceFilterAttribute), inherit: false)
            .Cast<Microsoft.AspNetCore.Mvc.ServiceFilterAttribute>()
            .Should().Contain(attribute => attribute.ServiceType == typeof(LoginEmailRateLimitFilter));
    }
}
